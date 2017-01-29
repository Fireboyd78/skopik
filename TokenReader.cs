using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Skopik
{
    internal class TokenReader : IDisposable
    {
        private int m_line = 0;

        private int m_tokenIndex = 0;
        private string[] m_tokenBuffer;
        
        protected bool IsBufferEmpty
        {
            get { return (m_tokenBuffer == null || (m_tokenBuffer.Length == 0)); }
        }
        
        protected StreamReader Reader { get; set; }
        
        public int Line
        {
            get { return m_line; }
        }
        
        public bool EndOfStream
        {
            get { return (Reader != null) ? (Reader.EndOfStream) ? (m_tokenIndex >= m_tokenBuffer.Length) : false : true; }
        }
        
        public void Dispose()
        {
            if (Reader != null)
                ((IDisposable)Reader).Dispose();
        }

        /// <summary>
        /// Reads in the tokens on the next line and returns the number of tokens loaded.
        /// </summary>
        /// <returns>The number of tokens parsed, otherwise -1 if end of stream reached.</returns>
        protected int ReadInTokens()
        {
            if (!Reader.EndOfStream)
            {
                // read in the next line of tokens
                var line = Reader.ReadLine();
                m_line++;

                // split them up into the token buffer and reset the index
                m_tokenBuffer = line.SplitTokensNew();
                m_tokenIndex = 0;

                // return number of tokens brought in
                return m_tokenBuffer.Length;
            }

            // end of stream
            return -1;
        }

        protected bool CheckToken(int tokenIndex)
        {
            // verifies index into the buffer is accessible
            if (!IsBufferEmpty)
                return (tokenIndex < m_tokenBuffer.Length);

            return false;
        }

        public int GetNumberOfTokens()
        {
            return (m_tokenBuffer != null) ? m_tokenBuffer.Length : -1;
        }
        
        public string GetToken(int tokenIndex)
        {
            var index = (m_tokenIndex + tokenIndex);

            if (CheckToken(index))
                return (m_tokenBuffer[index]);

            // failed to get token :(
            return null;
        }

        public string GetCurrentToken()
        {
            return GetToken(0);
        }

        public string GetNextToken()
        {
            string token = null;
            
            while (token == null)
            {
                // try getting the next token from the buffer
                token = GetToken(1);

                ++m_tokenIndex;

                // anything in the buffer?
                if (token == null)
                {
                    // ok, try filling in the buffer
                    // cancel if we reach EOF
                    if (ReadInTokens() == -1)
                        break;

                    // wraparound to the next set of tokens
                    m_tokenIndex = -1;
                }
            }

            return token;
        }

        public string PeekToken()
        {
            return GetToken(1);
        }

        public bool SkipToken()
        {
            return CheckToken(++m_tokenIndex);
        }

        public bool SkipLine()
        {
            return (ReadInTokens() != -1);
        }

        public bool MatchToken(string matchToken, string nestedToken)
        {
            var startLine = Line;
            
            // TODO: Fix this?
            if (nestedToken.Length != matchToken.Length)
                throw new InvalidOperationException("MatchToken() -- length of nested token isn't equal to the match token.");
            
            // did we find the match?
            var match = false;
            var token = "";

            while (!match && (token = GetNextToken()) != null)
            {
                if (token == matchToken)
                    match = true;
                if (token == nestedToken)
                {
                    var nestLine = Line;

                    // nested blocks
                    if (!MatchToken(matchToken, nestedToken))
                        throw new InvalidOperationException($"MatchToken() -- nested token '{nestedToken}' on line {nestLine} wasn't closed before the original token '{matchToken}' on line {startLine}.");
                }
            }
            
            return match;
        }
        
        public TokenReader(string filename)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException($"File '{filename}' not found, cannot instantiate a new TokenReader.");

            Reader = new StreamReader(filename, true);
        }

        public TokenReader(Stream stream)
        {
            if (!stream.CanRead || !stream.CanSeek)
                throw new EndOfStreamException("Cannot instantiate a new TokenReader on a closed/ended Stream.");

            Reader = new StreamReader(stream, true);
        }
    }
}
