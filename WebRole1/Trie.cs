using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    public class Trie
    {
        private Node root { get; set; }

        public Trie()
        {
            root = new Node("");
        }

        //Method used to check if a tree is empty, if the root node has not children
        //it is considered empty for our sake
        public bool emptyTree()
        {
            return (root.children.Count == 0);
        }

        //Add a word into the Trie Tree, takes in a string as a single argument
        public void addWord(string x)
        {
            if (x.Length > 0 && x != null)
            {
                addWordHelp(x, root);
            }
        }

        //Recursive Helper method for addWord that recursively breaks down a string
        //and continuely adds to the Trie Tree letter by letter
        //Takes the current substring and the current node of the tree in as arguments
        private void addWordHelp(string s, Node node)
        {
            try
            {
                //Stop when the word has been completely read through
                if (s.Length == 0 || s == null)
                {
                    node.isWord = true;
                }
                else
                {
                    string x = s.ToLower();
                    string curLetter = x.Substring(0, 1);
                    string word = x.Substring(1);
                    Dictionary<string, Node> children = node.children;
                    Node curNode;
                    if (children.ContainsKey(curLetter))
                    {
                        curNode = children[curLetter];
                    }
                    else
                    {
                        curNode = new Node(curLetter);
                        children.Add(curLetter, curNode);
                    }
                    addWordHelp(word, curNode);
                }
            }
            catch (Exception e)
            {

            }
        }

        //Search the tree for a list of words with the prefix containing the
        //string that is passed as a parameter
        public List<string> searchTree(string s)
        {
            if (s.Equals("") || s.Equals(null))
            {
                return new List<string>();
            }
            string x = s.ToLower();
            //Find the words end node first
            Node startNode = root;
            string cur = x;
            while (cur.Length > 0)
            {
                string letter = cur.Substring(0, 1);
                if (startNode.children.ContainsKey(letter))
                {
                    startNode = startNode.children[letter];

                }
                else
                {
                    return null;
                }
                cur = cur.Substring(1);
            }
            List<string> results = new List<string>();
            searchTreeHelp(x, results, startNode);
            return results;
        }

        //Recursive helper method used by SearchTree
        //Takes in a substring x, list of results, and current tree node as arguments
        private void searchTreeHelp(string prefix, List<string> results, Node node)
        {
            if (results.Count < 10)
            {
                if (node.isWord)
                {
                    results.Add(prefix);
                }
                if (results.Count < 10)
                {
                    Dictionary<string, Node> children = node.children;
                    if (children != null)
                    {
                        foreach (string key in children.Keys)
                        {
                            searchTreeHelp(prefix + children[key].letter, results, children[key]);
                        }
                    }
                }
            }
        }

        //Test Method finding all words in the trie tree
        public List<string> findAllWords()
        {
            List<string> words = new List<string>();
            findAllWordsHelp("", words, root);
            return words;
        }

        private void findAllWordsHelp(string prefix, List<string> results, Node node)
        {
            if (node.isWord)
            {
                results.Add(prefix);
            }
            Dictionary<string, Node> children = node.children;
            foreach (string key in children.Keys)
            {
                findAllWordsHelp(prefix + children[key].letter, results, children[key]);
            }

        }
    }
}