using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Skopik
{
    // internal class for reading .skop files quickly
    internal class SkopikReader : IDisposable
    {
        protected TokenReader Reader { get; }

        public void Dispose()
        {
            if (Reader != null)
                Reader.Dispose();
        }

        public SkopikObjectType ReadObject(SkopikBaseScopeType parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent), "Parent cannot be null.");

            var token = Reader.ReadToken();
            var dataType = Skopik.GetDataType(token);

            if (dataType == SkopikDataType.Invalid)
                Console.WriteLine($"ReadObject() -- Couldn't determine data type @ line {Reader.CurrentLine}: '{token}'.");
            
            // check for either strings or named scopes (e.g. ' "Inlined scope" : { ... } '
            if (dataType == SkopikDataType.String)
            {
                var strValue = token.StripQuotes();
                
                if (!Reader.EndOfLine)
                {
                    // peek for the scope assignment operator
                    var nextToken = Reader.PeekToken();

                    if (Skopik.IsScopeBlockOperator(nextToken))
                    {
                        Reader.PopToken();

                        // no need to peek for this one
                        nextToken = Reader.ReadToken();

                        // named scopes
                        if (Skopik.IsOpeningBrace(nextToken))
                        {
                            dataType = Skopik.GetControlDataType(token);

                            if (dataType == SkopikDataType.Array)
                                return ReadArray(strValue);
                            if (dataType == SkopikDataType.Scope)
                                return ReadScope(strValue);
                        }
                        else
                        {
                            throw new InvalidOperationException($"ReadObject() -- malformed data on line {Reader.CurrentLine}.");
                        }
                    }
                }
                
                return new SkopikStringType(strValue);
            }

            if (dataType == SkopikDataType.Reserved)
                Console.WriteLine($"Unknown data @ line {Reader.CurrentLine}: '{token}'");
            
            if (dataType == SkopikDataType.Boolean)
                return new SkopikBooleanType(token);

            if (dataType == SkopikDataType.Null)
                return new SkopikNullType();

            // anonymous scope/array?
            if (Skopik.IsOpeningBrace(token))
            {
                dataType = Skopik.GetControlDataType(token);

                if (dataType == SkopikDataType.Array)
                    return ReadArray();
                if (dataType == SkopikDataType.Scope)
                    return ReadScope();
            }

            //TODO: Strip prefix
            if (Skopik.IsNumberValue(dataType))
            {
                // strip suffix
                token = Skopik.StripDataTypeSuffix(token);

                if (dataType == SkopikDataType.Integer32)
                    return new SkopikInteger32Type(token);
                if (dataType == SkopikDataType.Integer64)
                    return new SkopikInteger64Type(token);

                if (dataType == SkopikDataType.UInteger32)
                    return new SkopikUInteger32Type(token);
                if (dataType == SkopikDataType.UInteger64)
                    return new SkopikInteger64Type(token);

                if (dataType == SkopikDataType.Float)
                    return new SkopikFloatType(token);
                if (dataType == SkopikDataType.Double)
                    return new SkopikDoubleType(token);
            }

            // we couldn't determine the data type, but let's not break the parser!
            return new SkopikNullType();
        }

        public SkopikObjectType ReadStatement(SkopikBaseScopeType parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent), "Parent cannot be null.");

            SkopikObjectType obj = new SkopikNullType();

            var name = Reader.ReadToken();

            if (!Reader.EndOfLine)
            {
                var op = Reader.PeekToken();

                if (Skopik.IsScopeBlockOperator(op))
                {
                    Reader.PopToken();

                    op = Reader.ReadToken();

                    // named scopes
                    if (Skopik.IsOpeningBrace(op))
                    {
                        var dataType = Skopik.GetControlDataType(op);
                        var scopeName = name.StripQuotes();

                        if (dataType == SkopikDataType.Array)
                        {
                            obj = ReadArray(scopeName);
                            name = $"<array::('{scopeName}')>";
                        }
                        if (dataType == SkopikDataType.Scope)
                        {
                            obj = ReadScope(scopeName);
                            name = $"<scope::('{scopeName}')>";
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"ReadObject() -- malformed data on line {Reader.CurrentLine}.");
                    }
                }
                else
                {
                    if (Skopik.IsAssignmentOperator(op))
                    {
                        // move past assignment operator
                        Reader.PopToken();
                    }
                    else
                    {
                        // move back to the beginning
                        Reader.Seek(-1);
                    }

                    // try reading the object normally
                    obj = ReadObject(parent);
                }

                if (!Reader.EndOfLine)
                {
                    var nextToken = Reader.PeekToken();

                    // move to next statement
                    if (Skopik.IsSeparator(nextToken, false))
                        Reader.PopToken();
                    if (Skopik.IsCommentLine(nextToken))
                        Reader.NextLine();
                }
            }

            if (parent is SkopikScopeType)
            {
                // add to the parent scope as a variable
                ((SkopikScopeType)parent).ScopeData.Add(name, obj);
            }

            return obj;
        }

        public SkopikArrayType ReadArray(string arrayName = "")
        {
            var array = new SkopikArrayType() {
                Name = arrayName
            };

            var maxIndex = -1;

            while (!Reader.EndOfStream)
            {
                var token = Reader.ReadToken();

                if (String.IsNullOrEmpty(token) || Skopik.IsCommentLine(token))
                    continue;

                SkopikObjectType obj = null;

                var index = (maxIndex + 1);
                var hasExplicitIndex = false;

                // explicit indice?
                if (Skopik.IsOpeningBrace(token))
                {
                    var braceIndex = Reader.FindNextPattern(new[] { "]", ":" });

                    if (braceIndex != -1)
                    {
                        var nextToken = Reader.ReadToken();
                        var dataType = Skopik.GetNumberDataType(nextToken);

                        if (dataType != SkopikDataType.Integer32)
                            throw new InvalidOperationException($"ReadArray() -- invalid explicit indice definition on line {Reader.CurrentLine}.");
                        if (Reader.TokenIndex != braceIndex)
                            throw new InvalidOperationException($"ReadArray() -- something went horribly wrong on line {Reader.CurrentLine}!");

                        // sanitize string
                        nextToken = Skopik.StripDataTypeSuffix(nextToken);

                        try
                        {
                            index = int.Parse(nextToken);
                        }
                        catch (Exception)
                        {
                            throw new InvalidOperationException($"ReadArray() -- something's rotten in denmark on line {Reader.CurrentLine}!");
                        }

                        if (index < 0)
                            throw new InvalidOperationException($"ReadAray() -- negative explicit indice on line {Reader.CurrentLine}.");
                        if (index <= maxIndex)
                            throw new InvalidOperationException($"ReadArray() -- explicit indice on line {Reader.CurrentLine} is less than the highest index.");

                        // move past the indice definition
                        Reader.PopToken(1);

                        hasExplicitIndex = true;
                    }
                }

                // end of current array?
                if (Skopik.IsClosingBrace(token))
                    break;
                
                if (hasExplicitIndex)
                {
                    // add null entries if needed
                    if ((index - 1) != maxIndex)
                    {
                        for (int i = (index - 1); i > maxIndex; i--)
                            array.ArrayData.Insert(i, null);
                    }
                }
                else
                {
                    // move back to the beginning
                    Reader.Seek(-1);
                }
                
                obj = ReadStatement(array);

                // stop if there's nothing left to read
                if (obj == null)
                    break;
                
                array.ArrayData.Insert(index, obj);

                if (!Reader.EndOfLine)
                {
                    var op = Reader.PeekToken();

                    // increase highest index
                    if (Skopik.IsSeparator(op, true))
                    {
                        Reader.PopToken();
                        maxIndex = index;
                    }
                }
            }

            return array;
        }

        public SkopikArrayType ReadNestedArray(SkopikBaseScopeType parent, string arrayName = "")
        {
            var array = ReadArray(arrayName);

            if (parent is SkopikScopeType)
                ((SkopikScopeType)parent).ScopeData.Add(arrayName, array);
            if (parent is SkopikArrayType)
                ((SkopikArrayType)parent).ArrayData.Add(array);

            return array;
        }
        
        public SkopikScopeType ReadScope(string scopeName = "")
        {
            var scope = new SkopikScopeType() {
                Name = scopeName
            };
            
            while (!Reader.EndOfStream)
            {
                var token = Reader.ReadToken();

                if (String.IsNullOrEmpty(token) || Skopik.IsCommentLine(token))
                    continue;

                SkopikObjectType obj = null;

                // end of current scope?
                if (Skopik.IsClosingBrace(token))
                    break;
               
                // move to beginning of statement
                Reader.Seek(-1);

                obj = ReadStatement(scope);

                // stop if there's nothing left to read
                if (obj == null)
                    break;
            }

            return scope;
        }

        public SkopikScopeType ReadNestedScope(SkopikBaseScopeType parent, string scopeName = "")
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent), "Parent cannot be null.");

            var scope = ReadScope(scopeName);

            if (parent is SkopikScopeType)
                ((SkopikScopeType)parent).ScopeData.Add(scopeName, scope);
            if (parent is SkopikArrayType)
                ((SkopikArrayType)parent).ArrayData.Add(scope);

            return scope;
        }
        
        public SkopikReader(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream), "Stream cannot be null.");

            Reader = new TokenReader(stream);
        }
    }
}
