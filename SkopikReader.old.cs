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
        private static readonly BindingFlags InternalBindingFlags =
            (BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);

        private Type m_readerType = typeof(StreamReader);

        private int m_column = 1; // "|BAZBAR" = 0, "FOOB|AR" = 4
        private int m_line = 1;

        // reset when new character read, true when AlignToken called
        private bool m_aligned = false;

        protected int CharPos
        {
            get { return (int)m_readerType.InvokeMember("charPos", InternalBindingFlags, null, Reader, null); }
        }

        protected int CharLen
        {
            get { return (int)m_readerType.InvokeMember("charLen", InternalBindingFlags, null, Reader, null); }
        }

        protected StreamReader Reader { get; }

        public int Column
        {
            get { return m_column; }
        }

        public int Line
        {
            get { return m_line; }
        }

        public long Position
        {
            get { return (Reader.BaseStream.Position - CharLen + CharPos); }
            set
            {
                Reader.BaseStream.Position = value;
                Reader.DiscardBufferedData();
            }
        }

        private string ReadStringInternal()
        {
            var result = "";

            var eos = false;

            var isOpen = false;
            var isEscaped = false;

            while (!Reader.EndOfStream && !eos)
            {
                var c = Peek();
                var flags = CharUtils.GetCharFlags(c);

                if (c == '\\')
                    isEscaped = true;

                if ((flags & CharacterTypeFlags.Quote) != 0)
                {
                    if (isOpen)
                    {
                        if (isEscaped)
                        {
                            // close escaped string
                            result += ReadStringInternal();
                            isEscaped = false;
                        }
                        else
                        {
                            eos = true;
                        }
                    }
                    else
                    {
                        if (isEscaped)
                        {
                            eos = true;
                        }
                        else
                        {
                            isOpen = true;
                        }
                    }
                }

                // read until newline is found (this is invalid)
                //if ((flags & CharacterTypeFlags.NewLine) != 0)
                //    break;

                if (isEscaped || isOpen)
                    result += (char)Read();
            }

            return result;
        }

        private void AlignToken()
        {
            // already aligned, dummy!
            if (m_aligned)
                return;

            var skipWhitespace = true;

            while (!Reader.EndOfStream && skipWhitespace)
            {
                var c = Peek();

                if (!CharUtils.HasCharFlags(c, CharacterTypeFlags.TabOrWhitespace | CharacterTypeFlags.NewLine))
                {
                    skipWhitespace = false;
                    continue;
                }

                // read whitespace and throw it away
                Read();
            }

            m_aligned = true;
        }

        private string ReadTokenInternal()
        {
            var result = "";

            AlignToken();

            while (!Reader.EndOfStream)
            {
                var c = Peek();
                var flags = CharUtils.GetCharFlags(c);

                // read until first whitespace or newline
                if ((flags & CharacterTypeFlags.EndOfToken) != 0)
                {
                    if ((flags & CharacterTypeFlags.NewLine) != 0)
                        m_line++;

                    break;
                }
                else if ((flags & CharacterTypeFlags.Quote) != 0)
                {
                    // strings should be parsed and then broken out of
                    result += ReadStringInternal();
                    break;
                }
                else
                {
                    result += (char)ReadInternal();
                }
            }

            return result;
        }

        public int Peek()
        {
            return Reader.Peek();
        }

        public string PeekToken()
        {
            var pos = Position;
            var token = ReadTokenInternal();

            // restore original position
            Position = pos;

            return token;
        }

        public void SkipToken()
        {
            // read a token and throw it away
            ReadTokenInternal();
        }

        private int ReadInternal()
        {
            m_aligned = false;
            return Reader.Read();
        }

        public int Read()
        {
            var c = ReadInternal();

            if (c == '\n')
            {
                m_column = 0;
                ++m_line;
            }

            return c;
        }

        public string Read(int length)
        {
            var result = new char[length];

            for (int i = 0; i < length; i++)
                result[i] = (char)Read();

            return new String(result);
        }

        public string ReadLine()
        {
            var line = Reader.ReadLine();

            m_column = 0;
            ++m_line;

            return line;
        }

        public string ReadToken()
        {
            // for cases where the string might be "|  var_something = 0" or "var_something =|  1"
            var token = PeekToken();

            // check for comment block
            if (Skopik.IsCommentBlock(token, false))
            {
                var startLine = Line;

                SkipToken();

                // parse comment blocks (as well as nested ones)
                if (!MatchToken(Skopik.CommentBlockCloseKey, Skopik.CommentBlockOpenKey))
                    throw new InvalidOperationException($"Unclosed multi-line comment on line {startLine}.");

                return ReadToken();
            }
            else if (Skopik.IsCommentLine(token))
            {
                // consume comment
                ReadLine();

                return ReadToken();
            }

            return ReadTokenInternal();
        }

        public string[] ReadLineTokens()
        {
            return ReadLine().SplitTokens();
        }

        public bool MatchToken(string matchToken, string nestedToken)
        {
            var startLine = Line;
            var startCol = Column;

            var strLen = matchToken.Length;

            // TODO: Fix this?
            if (nestedToken.Length != strLen)
                throw new InvalidOperationException("MatchToken() -- length of nested token isn't equal to the match token.");

            // hold the original position in case we don't find a match
            var holdPosition = Position;

            // did we find the match?
            var match = false;

            while (!Reader.EndOfStream && !match)
            {
                var startPos = Position;
                var token = Read(strLen);

                if (token == matchToken)
                    match = true;
                if (token == nestedToken)
                {
                    var nestLine = Line;
                    var nestCol = Column;

                    // nested blocks
                    if (!MatchToken(matchToken, nestedToken))
                        throw new InvalidOperationException($"MatchToken() -- nested token at ({nestLine},{nestCol}) wasn't closed before the original token at ({startLine},{startCol})");
                }
            }

            if (!match)
            {
                // restore the position since we failed
                Position = holdPosition;
            }

            return match;
        }

        public SkopikObjectType ReadObject(SkopikBaseScopeType parent)
        {
            var startLine = Line;
            var startPos = Position;

            var token = ReadToken();
            var dataType = Skopik.GetDataType(token);

            if (dataType == SkopikDataType.Invalid)
                Console.WriteLine($"ReadObject() -- Couldn't determine data type @ line {startLine}: '{token}'.");

            // check for either strings or named scopes (e.g. ' "Inlined scope" : { ... } '
            if (dataType == SkopikDataType.String)
            {
                // peek for the scope assignment operator
                var nextToken = PeekToken();
                var strValue = token.StripQuotes();

                // if it's an inline named scope, reset the position and do a ReadScope instead
                if (Skopik.IsScopeBlockOperator(nextToken))
                {
                    Position = startPos;

                    // will add any children to the scope data
                    return ReadScope(parent, strValue);
                }

                return new SkopikStringType(strValue);
            }

            if (dataType == SkopikDataType.Reserved)
                Console.WriteLine($"Unknown data @ line {startLine}: '{token}'");

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
            var startLine = Line;

            SkopikObjectType obj = new SkopikNullType();

            var peekOp = PeekToken();

            if (Skopik.GetControlDataType(peekOp) == SkopikDataType.Scope)
                SkipToken();

            AlignToken();

            if (parent is SkopikScopeType)
            {
                var nameCol = Column;
                var nameLine = Line;

                var name = ReadToken();

                //Console.WriteLine($"ReadStatement() -- ['{name}'] @ {startLine}");

                // operator must be on same line
                var op = ((nameLine == Line) || (Column >= nameCol)) ? ReadToken() : "";

                if (op.Length > 1)
                {
                    if ((Skopik.GetOperatorDataType(name) == SkopikDataType.Reserved))
                    {
                        //Console.WriteLine($"Found special operator @ line {startLine}: '{name} {op}'");
                    }
                    else
                    {
                        throw new InvalidOperationException($"ReadStatement() -- Malformed statement @ line {startLine}.");
                    }
                }

                if (op.Length != 0)
                {
                    if (Skopik.IsAssignmentOperator(op))
                        obj = ReadObject(parent);
                    if (Skopik.IsScopeBlockOperator(op))
                    {
                        var scopeName = name.StripQuotes();
                        return ReadScope(parent, scopeName);
                    }

                    var nextToken = PeekToken();

                    if (nextToken.Length > 0)
                    {
                        // move to next statement
                        if (Skopik.IsSeparator(nextToken, false))
                            SkipToken();
                        if (Skopik.IsCommentLine(nextToken))
                            ReadLine();
                        if (Skopik.GetControlDataType(nextToken) == SkopikDataType.Scope)
                            SkipToken();
                    }
                }

                ((SkopikScopeType)parent).ScopeData.Add(name, obj);
            }
            else
            {
                throw new InvalidOperationException("unsupported parent ;(");
            }

            return obj;
        }

        public SkopikArrayType ReadArray(SkopikBaseScopeType parent)
        {
            var startLine = Line;

            // we skip arrays for now since I'm not entirely sure how to parse them
            // assuming the array is already opened...
            if (!MatchToken("]", "["))
                throw new InvalidOperationException($"ReadArray() -- Unclosed array @ line {startLine}.");

            return new SkopikArrayType("_ARRAY_");
        }

        public SkopikScopeType ReadScope(SkopikBaseScopeType parent, string scopeName = "")
        {
            var scope = new SkopikScopeType() {
                Name = scopeName
            };

            while (!Reader.EndOfStream)
            {
                var startLine = Line;
                var statement = ReadStatement(scope);
            }

            if (parent is SkopikScopeType)
                ((SkopikScopeType)parent).ScopeData.Add(scopeName, scope);
            if (parent is SkopikArrayType)
                ((SkopikArrayType)parent).ArrayData.Add(scope);

            return scope;
        }

        public SkopikReader(StreamReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader), "Reader cannot be null.");

            Reader = reader;
        }
    }
}
