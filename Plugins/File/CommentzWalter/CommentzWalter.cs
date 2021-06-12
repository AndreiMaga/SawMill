using PluginInterface.Common.File;
using PluginInterface.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CommentzWalter
{
    class CommentzWalter
    {

        public class Result
        {
            public byte[] Header;
            public int Index;
            public string Type;
            public string Name;
        }
        class Node
        {
            public byte Character { get; set; }
            public int Depth { get; set; }
            public byte[] Word { get; set; }

            public Node Parent { get; set; }
            public string Type { get; set; }
            public string Name { get; set; }

            public List<Node> Children { get; set; }

            public int Shift1 { get; set; }
            public int Shift2 { get; set; }

            public Node(byte character, int depth, Node parent)
            {
                Character = character;
                Depth = depth;
                Parent = parent;
                Word = null;
                Children = new();
            }
        }


        class Trie
        {
            public int Size { get; set; }
            public Node Root { get; set; }

            protected Trie()
            {
                Size = 0;
                Root = CreateNode(0, 0, null);
            }

            public static Node CreateNode(byte character, int depth, Node parent) => new(character, depth, parent);

            public void AddWord(byte[] word, string type = "Header", string name = "")
            {
                var currentNode = Root;
                var currentDepth = 1;

                foreach (var c in word)
                {
                    var nextNode = currentNode.Children.Find(child => child.Character == c);

                    if (nextNode == null)
                    {
                        nextNode = CreateNode(c, currentDepth, currentNode);
                        currentNode.Children.Add(nextNode);
                    }

                    currentNode = nextNode;
                    currentDepth += 1;

                }

                if (currentNode.Word != null)
                {
                    return;
                }

                currentNode.Word = word;
                currentNode.Type = type;
                currentNode.Name = name;
                Size += 1;

            }

            public static bool IsRoot(Node node)
            {
                return node.Character == 0;
            }

            public static Node NodeHasChild(Node node, byte c)
            {
                return node.Children.Find(child => child.Character == c);
            }

        }

        class CWTrie : Trie
        {
            private int MinDepth { get; set; }
            private Dictionary<byte, int> Char_lookup_table { get; set; }

            public CWTrie() : base()
            {
                MinDepth = -1;
                Char_lookup_table = new();
            }

            public new void AddWord(byte[] word, string type = "Header", string name = "")
            {

                base.AddWord(word, type, name);
                var pos = 1;


                foreach (byte c in word)
                {
                    if (Char_lookup_table.TryGetValue(c, out int minCharDepth) == false || minCharDepth > pos) // minCharDepth == null
                    {
                        Char_lookup_table[c] = pos;
                    }
                    pos += 1;
                }

                MinDepth = MinDepth == -1 ? word.Length : (MinDepth > word.Length ? word.Length : MinDepth);

            }

            public void InitializeShift()
            {
                Queue<Node> nodes = new(Root.Children);
                Root.Shift1 = 1;
                Root.Shift2 = MinDepth;

                while (nodes.Count > 0)
                {
                    var currentNode = nodes.Dequeue();

                    currentNode.Shift1 = MinDepth;
                    currentNode.Shift2 = currentNode.Parent.Shift2;
                    currentNode.Children.ForEach(child => nodes.Enqueue(child));

                }

            }

            private int CharFunction(byte c)
            {
                if (Char_lookup_table.TryGetValue(c, out int min) == false)
                {
                    min = MinDepth - 1;// min == null
                }
                return min;

            }

            private int ShiftFunction(Node node, int j)
            {
                int max;
                switch (node.Character)
                {
                    case 0:
                        max = node.Shift1;
                        break;
                    default:
                        {
                            var k = CharFunction(node.Character) - j - 1;
                            max = node.Shift1 > k ? node.Shift1 : k;
                        }
                        break;
                }
                return max > node.Shift2 ? node.Shift2 : max;

            }

            public List<Result> ReportAllMatches(Stream stream, int bufferLen = 1024 * 1024)
            {
                int i = MinDepth - 1;
                List<Result> matches = new();
                var v = Root;
                int j = 0;

                int read;
                int offset = 0;
                byte[] buffer = new byte[bufferLen];
                while((read = stream.Read(buffer, 0, bufferLen)) > 0)
                {
                    while( i < read)
                    {
                        Node nodehasChild;
                        while (i + j < buffer.Length && ((nodehasChild = NodeHasChild(v, buffer[i + j])) != null))
                        {
                            v = nodehasChild;
                            j += 1;

                            if (v.Word != null)
                            {

                                matches.Add(new Result
                                {
                                    Header = v.Word,
                                    Index = offset + i,
                                    Type = v.Type,
                                    Name = v.Name
                                });
                            }


                            // check if we can read while matching
                            if(i+j >= bufferLen && stream.Position != stream.Length)
                            {
                                
                                int lastRead = read;
                                if((read = stream.Read(buffer, 0, bufferLen)) > 0)
                                {
                                    offset += lastRead;
                                }

                            }
                        }
                        if (j > i)
                        {
                            j = i;
                        }
                        i += ShiftFunction(v, j);
                        j = 0;
                        v = Root;
                    }
                    offset += read;
                    
                }

                return matches;
            }
        }


        private void RunAsync(FileInfo file, KeyValuePair<string, List<PluginInterface.Common.File.File>> f)
        {
            List<Result> results = new();

            List<string> toFind = new();
            CWTrie cw = new();
            foreach (var h in f.Value)
            {
                cw.AddWord(FileHeaders.ByteArrayFromString(h.Header), "Header", f.Key);
                cw.AddWord(FileHeaders.ByteArrayFromString(h.Footer), "Footer", f.Key);
            }


            cw.InitializeShift();
            var stream = file.OpenRead();

            List<Dictionary<byte[], List<int>>> m = new();

            var matches = cw.ReportAllMatches(stream);

            lock (FinalResults)
            {
                matches.ForEach(m =>
                {

                    if (FinalResults.ContainsKey(m.Name))
                    {
                        FinalResults[m.Name].Add(m);
                    }
                    else
                    {
                        FinalResults.Add(m.Name, new List<Result> { m });
                    }

                });
            }

        }


        public Dictionary<string, List<Result>> FinalResults = new();
        public CommentzWalter(FileInfo file, Logger logger)
        {
            List<Task> taskList = new();
            // search for each type
            var headerDict = FileHeaders.Headers.File.GroupBy(k => k.Name).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var h in headerDict)
            {
                Task t = new Task(() =>
                {
                    RunAsync(file, h);
                });
                t.Start();
                taskList.Add(t);
            }
            logger.Information(string.Format("Started {0} tasks for {1} file types", taskList.Count, headerDict.Count));
            Task.WaitAll(taskList.ToArray());
        }

    }
}
