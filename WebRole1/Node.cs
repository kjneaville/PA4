using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    /// <summary>
    /// Trie Tree Node data structure, holds a string letter of the current node,
    /// Dictionary of String letters pointing to other Trie nodes
    /// </summary>
    public class Node
    {
        public string letter { get; set; }
        public bool isWord { get; set; }
        public Dictionary<String, Node> children { get; set; }

        public Node(string letter)
        {
            this.letter = letter;
            this.children = new Dictionary<string, Node>();
            this.isWord = false;
        }
    }
}