using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var dataType = SkopikDataType.None;

            try
            {
                dataType = Skopik.GetAnyDataType(token);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"ReadObject() -- horrific failure on line {Reader.CurrentLine}, couldn't parse '{token}'!", e);
            }

            if (dataType == SkopikDataType.None)
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
                            if (Skopik.IsDataType(nextToken, SkopikDataType.OpArrayOpen))
                                return ReadArray(strValue);
                            if (Skopik.IsDataType(nextToken, SkopikDataType.OpScopeOpen))
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

            if (dataType == SkopikDataType.Keyword)
                Debug.WriteLine($"Unknown data @ line {Reader.CurrentLine}: '{token}'");
            
            if (dataType == SkopikDataType.Boolean)
                return new SkopikBooleanType(token);

            if (dataType == SkopikDataType.Null)
                return new SkopikNullType();

            // anonymous scope/array?
            if (Skopik.IsOpeningBrace(token))
            {
                if (Skopik.IsDataType(token, SkopikDataType.OpArrayOpen))
                    return ReadArray();
                if (Skopik.IsDataType(token, SkopikDataType.OpScopeOpen))
                    return ReadScope();
            }

            if (Skopik.IsDecimalNumberValue(dataType))
            {
                var decimalToken = Skopik.SanitizeNumber(token, false);

                if (String.IsNullOrEmpty(decimalToken))
                    throw new InvalidOperationException($"ReadObject() -- could not sanitize decimal token '{token}' on line {Reader.CurrentLine}.");
                
                switch (dataType)
                {
                case SkopikDataType.Float:
                    {
                        var val = Convert.ToSingle(decimalToken);
                        return new SkopikFloatType(val);
                    }
                case SkopikDataType.Double:
                    {
                        var val = Convert.ToDouble(decimalToken);
                        return new SkopikDoubleType(val);
                    }
                }
            }

            if (Skopik.IsNumberValue(dataType))
            {
                var isNegative = Skopik.IsNegativeNumber(token);
                var strIndex = 0;

                SkopikIntegerBaseType number = null;

                if ((dataType & SkopikDataType.BitField) != 0)
                {
                    if ((dataType & (SkopikDataType.NumberFlagMask & ~SkopikDataType.BitField)) != 0)
                        throw new InvalidOperationException($"ReadObject() -- invalid binary token '{token}' on line {Reader.CurrentLine}.");

                    if (isNegative)
                        strIndex += 1;

                    var binaryToken = Skopik.SanitizeNumber(token.Substring(strIndex), false);

                    if (String.IsNullOrEmpty(binaryToken))
                        throw new InvalidOperationException($"ReadObject() -- could not sanitize binary token '{token}' on line {Reader.CurrentLine}.");
                    
                    if (isNegative)
                    {
                        var val = Convert.ToInt32(binaryToken, 2);

                        if (isNegative)
                            val = -val;

                        number = new SkopikInteger32Type(val);
                    }
                    else
                    {
                        var val = Convert.ToUInt32(binaryToken, 2);
                        number = new SkopikUInteger32Type(val);
                    }

                    number.DisplayType = SkopikIntegerDisplayType.Binary;
                }
                else
                {
                    // don't know what happened!
                    if (!Enum.IsDefined(typeof(SkopikDataType), dataType))
                        throw new InvalidOperationException($"ReadObject() -- invalid number token '{token}' on line {Reader.CurrentLine}.");

                    var isHex = Skopik.IsHexadecimalNumber(token);

                    if (isHex)
                    {
                        if (isNegative)
                            strIndex += 1;

                        strIndex += 2;   
                    }

                    var numberToken = Skopik.SanitizeNumber(token.Substring(strIndex), isHex);
                    
                    if (String.IsNullOrEmpty(numberToken))
                        throw new InvalidOperationException($"ReadObject() -- could not sanitize number token '{token}' on line {Reader.CurrentLine}.");

                    var numberBase = (!isHex) ? 10 : 16;
                    
                    switch (dataType)
                    {
                    case SkopikDataType.Integer32:
                        {
                            var val = Convert.ToInt32(numberToken, numberBase);

                            if (isHex && isNegative)
                                val = -val;

                            number = new SkopikInteger32Type(val);
                        }
                        break;
                    case SkopikDataType.Integer64:
                        {
                            var val = Convert.ToInt64(numberToken, numberBase);

                            if (isHex && isNegative)
                                val = -val;

                            number = new SkopikInteger64Type(val);
                        }
                        break;
                    case SkopikDataType.UInteger32:
                        {
                            var val = Convert.ToUInt32(numberToken, numberBase);
                            number = new SkopikUInteger32Type(val);
                        }
                        break;
                    case SkopikDataType.UInteger64:
                        {
                            var val = Convert.ToUInt64(numberToken, numberBase);
                            number = new SkopikUInteger64Type(val);
                        }
                        break;
                    }

                    if (number == null)
                        throw new InvalidOperationException($"ReadObject() -- the resulting value for '{token}' on line {Reader.CurrentLine} was null.");
                    
                    if (isHex)
                        number.DisplayType = SkopikIntegerDisplayType.Hexadecimal;
                }

                return number;
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
                        var scopeName = name.StripQuotes();

                        if (Skopik.IsDataType(op, SkopikDataType.OpArrayOpen))
                        {
                            obj = ReadArray(scopeName);
                            name = $"<array::('{scopeName}')>";
                        }
                        else  if (Skopik.IsDataType(op, SkopikDataType.OpScopeOpen))
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
                    if (Skopik.IsScopeSeparator(nextToken))
                        Reader.PopToken();
                    if (Tokenizer.IsCommentLine(nextToken))
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

                if (String.IsNullOrEmpty(token))
                    continue;
                
                SkopikObjectType obj = null;

                var index = (maxIndex + 1);
                var hasExplicitIndex = false;

                // explicit indice?
                if (Skopik.IsOpeningBrace(token))
                {
                    if (Reader.FindNextPattern(new[] { "]", ":" }) != -1)
                    {
                        var nextToken = Reader.ReadToken();
                        var dataType = Skopik.GetNumberDataType(nextToken);

                        var strIndex = 0;

                        if (dataType != SkopikDataType.Integer32)
                            throw new InvalidOperationException($"ReadArray() -- invalid explicit indice definition on line {Reader.CurrentLine}.");

                        var isHex = Skopik.IsHexadecimalNumber(nextToken);

                        if (isHex)
                            strIndex += 2;

                        nextToken = Skopik.SanitizeNumber(nextToken.Substring(strIndex), isHex);

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
                    for (int i = (maxIndex + 1); i < index; i++)
                        array.ArrayData.Insert(i, null);
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
                    if (Skopik.IsArraySeparator(op))
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

                if (String.IsNullOrEmpty(token))
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
