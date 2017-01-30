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
            
            if (parent is SkopikScopeType)
            {
                var name = Reader.ReadToken();
                
                if (!Reader.EndOfLine)
                {
                    var op = Reader.PeekToken();

                    if (Skopik.IsAssignmentOperator(op))
                    {
                        // move past the assignment operator
                        Reader.PopToken();
                        obj = ReadObject(parent);
                    }

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

                // add to the parent scope as a variable
                ((SkopikScopeType)parent).ScopeData.Add(name, obj);
            }
            else
            {
                throw new InvalidOperationException("unsupported parent ;(");
            }

            return obj;
        }

        public SkopikArrayType ReadArray(string arrayName = "")
        {
            var aryStart = Reader.CurrentLine;

            // we skip arrays for now since I'm not entirely sure how to parse them
            // assuming the array is already opened...
            if (!Reader.MatchToken("]", "["))
                throw new InvalidOperationException($"ReadArray() -- Unclosed array @ line {aryStart}.");

            var aryEnd = Reader.CurrentLine;

            if (aryStart == aryEnd)
                aryEnd = -1;

            return new SkopikArrayType($"<array::[{aryStart},{aryEnd}]>");
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
