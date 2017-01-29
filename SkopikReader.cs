using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Skopik
{
    // internal class for reading .skop files quickly
    internal class SkopikReader
    {
        protected TokenReader Reader { get; }
        
        public SkopikObjectType ReadObject(SkopikBaseScopeType parent)
        {
            var token = Reader.GetNextToken();            
            var dataType = Skopik.GetDataType(token);

            if (dataType == SkopikDataType.Invalid)
                Console.WriteLine($"ReadObject() -- Couldn't determine data type @ line {Reader.Line}: '{token}'.");

            // check for either strings or named scopes (e.g. ' "Inlined scope" : { ... } '
            if (dataType == SkopikDataType.String)
            {
                // peek for the scope assignment operator
                var nextToken = Reader.PeekToken();
                var strValue = token.StripQuotes();

                if (!String.IsNullOrEmpty(nextToken))
                {
                    // if it's an inline named scope, read the scope
                    if (Skopik.IsScopeBlockOperator(nextToken))
                    {
                        // will add any children to the scope data
                        return ReadScope(parent, strValue);
                    }
                }

                return new SkopikStringType(strValue);
            }

            if (dataType == SkopikDataType.Reserved)
                Console.WriteLine($"Unknown data @ line {Reader.Line}: '{token}'");

            if (dataType == SkopikDataType.Array)
                return ReadArray(parent);
            if (dataType == SkopikDataType.Scope)
                return ReadScope(parent);

            if (dataType == SkopikDataType.Boolean)
                return new SkopikBooleanType(token);

            if (dataType == SkopikDataType.Null)
                return new SkopikNullType();

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
            SkopikObjectType obj = new SkopikNullType();
            
            if (parent is SkopikScopeType)
            {
                var token = Reader.GetCurrentToken();
                var op = Reader.PeekToken();

                if (!String.IsNullOrEmpty(op))
                {
                    Reader.SkipToken();

                    if (Skopik.IsAssignmentOperator(op))
                        obj = ReadObject(parent);
                    if (Skopik.IsScopeBlockOperator(op))
                    {
                        var scopeName = Reader.GetToken(-1).StripQuotes();
                        obj = ReadScope(parent, scopeName);
                    }
                    
                    var nextToken = Reader.PeekToken();

                    if (!String.IsNullOrEmpty(nextToken))
                    {
                        // move to next statement
                        if (Skopik.IsSeparator(nextToken, false))
                            Reader.SkipToken();
                        if (Skopik.IsCommentLine(nextToken))
                            Reader.SkipLine();
                        if (Skopik.GetControlDataType(nextToken) == SkopikDataType.Scope)
                            Reader.SkipToken();
                    }
                }

                if (!(obj is SkopikScopeType))
                    ((SkopikScopeType)parent).ScopeData.Add(token, obj);
            }
            else
            {
                throw new InvalidOperationException("unsupported parent ;(");
            }

            return obj;
        }

        public SkopikArrayType ReadArray(SkopikBaseScopeType parent)
        {
            // we skip arrays for now since I'm not entirely sure how to parse them
            // assuming the array is already opened...
            if (!Reader.MatchToken("]", "["))
                throw new InvalidOperationException($"ReadArray() -- Unclosed array @ line {Reader.Line}.");

            return new SkopikArrayType("_ARRAY_");
        }

        public SkopikScopeType ReadScope(SkopikBaseScopeType parent, string scopeName = "")
        {
            var scope = new SkopikScopeType() {
                Name = scopeName
            };

            int nestLevel = 0;
            
            while (!Reader.EndOfStream)
            {
                var token = Reader.GetNextToken();

                if (String.IsNullOrEmpty(token))
                    continue;

                SkopikObjectType obj = null;

                if (Skopik.GetControlDataType(token) == SkopikDataType.Scope)
                {
                    if (Skopik.IsOpeningBrace(token))
                    {
                        var cTok = Reader.GetToken(-1);

                        if (Skopik.IsScopeBlockOperator(cTok))
                            continue;

                        // new scope
                        ++nestLevel;
                    }

                    Reader.SkipToken();

                    if (Skopik.IsClosingBrace(token))
                    {
                        if (nestLevel > 0)
                            --nestLevel;

                        if (nestLevel == 0)
                            break;
                    }
                }

                obj = ReadStatement(scope);

                if (obj == null)
                    break;
            }

            if (parent is SkopikScopeType)
                ((SkopikScopeType)parent).ScopeData.Add(scopeName, scope);
            if (parent is SkopikArrayType)
                ((SkopikArrayType)parent).ArrayData.Add(scope);

            return scope;
        }

        public SkopikReader(TokenReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader), "Reader cannot be null.");

            Reader = reader;
        }
    }
}
