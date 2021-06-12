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

            public Node ACSuffixLink { get; set; }

            public Node ACOutputLink { get; set; }

            public Node CWSuffixLink { get; set; }
            public Node CWOutputLink { get; set; }

            public List<Node> Children { get; set; }

            public int MinDifference_s1 { get; set; }
            public int MinDifference_s2 { get; set; }

            public int Shift1 { get; set; }
            public int Shift2 { get; set; }

            public Node(byte character, int depth, Node parent)
            {
                Character = character;
                Depth = depth;
                Parent = parent;
                Word = null;
                ACSuffixLink = null;
                ACOutputLink = null;
                Children = new();
                MinDifference_s1 = -1;
                MinDifference_s2 = -1;
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

            public static Node GetSuffixLink(Node node)
            {
                var searcher = node.Parent.ACSuffixLink;

                Node nodehasChild = null;

                while (!IsRoot(searcher) && !((nodehasChild = NodeHasChild(searcher, node.Character)) != null))
                {
                    searcher = searcher.ACSuffixLink;
                    if (searcher == null)
                    {
                        throw new Exception();
                    }
                }

                if (nodehasChild != null)
                {
                    return nodehasChild;
                }
                else
                {
                    if (!IsRoot(searcher))
                    {
                        throw new Exception();
                    }
                    return searcher;
                }

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

                    currentNode.Shift1 = currentNode.CWSuffixLink == null ? MinDepth : currentNode.MinDifference_s1;
                    currentNode.Shift2 = currentNode.CWOutputLink == null ? currentNode.Parent.Shift2 : currentNode.MinDifference_s2;
                    currentNode.Children.ForEach(child => nodes.Enqueue(child));

                }

            }

            public void CreateFailureLinks()
            {
                Queue<Node> nodes = new();

                Root.Children.ForEach(child =>
                {
                    child.ACSuffixLink = Root;
                    child.Children.ForEach(ch => nodes.Enqueue(ch));
                });


                while (nodes.Count > 0)
                {
                    var currentNode = nodes.Dequeue();
                    currentNode.Children.ForEach(ch => nodes.Enqueue(ch));
                    var ACSuffixNode = GetSuffixLink(currentNode);
                    var suffixIsWord = ACSuffixNode.Word != null;
                    currentNode.ACSuffixLink = ACSuffixNode;

                    currentNode.ACOutputLink = suffixIsWord ? ACSuffixNode : ACSuffixNode.ACOutputLink;

                    var isSet = currentNode.Word != null;

                    if (ACSuffixNode.MinDifference_s1 == -1 || ACSuffixNode.MinDifference_s1 > currentNode.Depth - ACSuffixNode.Depth)
                    {
                        ACSuffixNode.MinDifference_s1 = currentNode.Depth - ACSuffixNode.Depth;
                        ACSuffixNode.ACSuffixLink = currentNode;
                    }

                    if (isSet)
                    {
                        if (ACSuffixNode.MinDifference_s2 == -1 || ACSuffixNode.MinDifference_s2 > currentNode.Depth - ACSuffixNode.Depth)
                        {
                            ACSuffixNode.MinDifference_s2 = currentNode.Depth - ACSuffixNode.Depth;
                            ACSuffixNode.ACOutputLink = currentNode;
                        }
                    }

                }
                InitializeShift();
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

            public List<Result> ReportAllMatches(byte[] text, int offset)
            {
                int i = MinDepth - 1;
                List<Result> matches = new();
                var v = Root;
                int j = 0;
                while (i < text.Length)
                {

                    Node nodehasChild;
                    while (i+j < text.Length &&((nodehasChild = NodeHasChild(v, text[i + j])) != null))
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

                    }
                    if (j > i)
                    {
                        j = i;
                    }
                    i += ShiftFunction(v, j);
                    j = 0;
                    v = Root;
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


            cw.CreateFailureLinks();
            var stream = file.OpenRead();

            int len = 1024 * 1024;

            byte[] arr = new byte[len];
            List<Dictionary<byte[], List<int>>> m = new();
            int read = 0;
            int offset = 0;
            while ((read = stream.Read(arr, 0, len)) > 0)
            {
                var matches = cw.ReportAllMatches(arr, offset);
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
                offset += read;
            }

        }


        public Dictionary<string,List<Result>> FinalResults = new();
        public CommentzWalter(FileInfo file, Logger logger)
        {
            List<Task> taskList = new();
            // search for each type
            foreach (var f in FileHeaders.Headers.File.GroupBy(k => k.Name).ToDictionary(g => g.Key, g => g.ToList()))
            {
                Task t = new Task(() =>
                {
                    RunAsync(file, f);
                });
                t.Start();
                taskList.Add(t);
            }

            Task.WaitAll(taskList.ToArray());
        }

    }
}
