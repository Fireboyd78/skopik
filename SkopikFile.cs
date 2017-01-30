using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Skopik
{
    public class SkopikFile
    {
        private SkopikScopeType _globalScope;

        public string FileName { get; }

        public SkopikScopeType GlobalScope
        {
            get
            {
                if (_globalScope == null)
                    _globalScope = new SkopikScopeType() {
                        Name = "<global>"
                    };

                return _globalScope;
            }
        }
        
        public void Parse()
        {
            using (var ms = new MemoryStream(File.ReadAllBytes(FileName)))
            using (var skop = new SkopikReader(ms))
            {
                skop.ReadNestedScope(GlobalScope, $"<scope::('{Path.GetFileNameWithoutExtension(FileName)}')>");
            }
        }
        
        /*
            TODO: split this up into multiple parts?
        */
        public void ParseOLD()
        {
            using (var reader = new StreamReader(FileName, true))
            {
                var line = "";
                var lineNumber = 0;

                // reads line and increments line number
                var _readLine = new Action(() => {
                    if (reader.EndOfStream)
                        throw new InvalidOperationException("readLine() : tried to read past end of stream!");
                    line = reader.ReadLine()?.Trim(' ', '\t'); // removes any whitespace and tabs from the beginning and end
                    ++lineNumber;
                });

                // skips comments
                // returns true if a comment or whitespace was parsed, otherwise false
                var _skipWhiteSpaceAndComments = new Func<bool>(() => {
                    // check for the blatantly obvious first :P
                    if (String.IsNullOrWhiteSpace(line))
                        return true;

                    /*
                    for (int i = 0; i < CommentKeys.Length; i += 2)
                    {
                        var openTag = CommentKeys[i];
                        var closeTag = CommentKeys[i + 1];

                        if (line.StartsWith(openTag))
                        {
                            var startLine = lineNumber;

                            if (closeTag != null)
                            {
                                var closed = false;

                                while (!closed && !reader.EndOfStream)
                                {
                                    if (line.EndsWith(closeTag))
                                    {
                                        // don't move to the next line, that's not our job!
                                        closed = true;
                                        break;
                                    }

                                    _readLine();
                                }

                                if (!closed)
                                    throw new InvalidOperationException($"Unclosed multi-line comment on line {startLine}.");
                            }

                            // successfully parsed a comment
                            return true;
                        }
                    }
                    */

                    // no comments parsed
                    return false;
                });

                var _moveNextLine = new Action(() => {
                    _readLine();

                    while (_skipWhiteSpaceAndComments())
                        _readLine();
                });
                
                while (!reader.EndOfStream)
                {
                    _moveNextLine();

                    if (line.IndexOf('=') != -1)
                    {
                        if (line.IndexOf(',') != -1)
                        {
                            //Console.WriteLine($"Malformed statement @ line {lineNumber}, skipping...");
                            continue;
                        }

                        var statements = line.Split(';');

                        foreach (var statement in statements)
                        {
                            // empty statement, NOT an error!
                            if (statement.Length == 0)
                                continue;

                            // TODO: Write custom method (this can't handle inline comments!)
                            var s = statement.Split('=').Select((t) => t.Trim(' ', '\t')).ToArray();

                            if (s.Length < 2)
                            {
                                //Console.WriteLine($"Unknown statement @ line {lineNumber}, skipping...");
                                continue;
                            }

                            var k = s[0];
                            var v = s[1];

                            var vType = Skopik.GetDataType(v);

                            if (vType == SkopikDataType.Invalid)
                            {
                                //Console.WriteLine($"Invalid statement assignment data type @ line {lineNumber}, skipping...");
                                continue;
                            }
                            else
                            {
                                //Console.WriteLine($"Successfully determined data type '{vType}' @ line {lineNumber}!");
                            }

                            //Console.WriteLine($" - Name: {k}");
                        }
                    }
                    else if (line.IndexOf(':') != -1)
                    {
                        // TODO: Write custom method (this can't handle inline comments!)
                        var statement = line.Split(':').Select((t) => t.Trim(' ', '\t')).ToArray();

                        if (statement.Length < 2)
                            throw new InvalidOperationException($"Malformed scope statement @ line {lineNumber}!");

                        var n = statement[0];
                        var s = statement[1];

                        var nType = Skopik.GetDataType(n);
                        var sType = Skopik.GetDataType(s);

                        if (nType == SkopikDataType.String)
                        {
                            switch (sType)
                            {
                            case SkopikDataType.Array:
                                //Console.WriteLine($"Successfully parsed Array statement @ line {lineNumber}!");
                                break;
                            case SkopikDataType.Scope:
                                //Console.WriteLine($"Successfully parsed Scope statement @ line {lineNumber}!");
                                break;
                            default:
                                throw new InvalidOperationException($"Malformed scope statement @ line {lineNumber}, got unknown type '{sType}'.");
                            }

                            //Console.WriteLine($" - Name: {n.Trim('"')}");
                        }
                        else
                        {
                            throw new InvalidOperationException($"Malformed scope name type, got '{nType}' @ line {lineNumber}.");
                        }
                    }
                    else
                    {
                        switch (Skopik.GetDataType(line))
                        {
                        case SkopikDataType.Reserved:
                            //Console.WriteLine($"Skipping reserved statement @ line {lineNumber}.");
                            continue;
                        case SkopikDataType.Invalid:
                            //Console.WriteLine($"Unknown statement type @ line {lineNumber}! Skipping...");
                            continue;
                        }
                    }
                }

                //Console.WriteLine($"Parsed SKOP file to line {lineNumber}.");
            }
        }

        public SkopikFile(string fileName)
        {
            if (!File.Exists(fileName))
                throw new InvalidOperationException("Skopik file not found!");

            FileName = fileName;
        }
    }
}
