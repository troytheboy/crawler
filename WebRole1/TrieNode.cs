using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    /// <summary>
    /// TrieNode is an internal object to encapsulate recursive, helper etc. methods. 
    /// </summary>
    public class TrieNode
    {
        #region data members

        /// <summary>
        /// The character for the TrieNode.
        /// </summary>
        internal char Character { get; private set; }

        /// <summary>
        /// Children Character->TrieNode map.
        /// </summary>
        LinkedList<TrieNode> Children { get; set; }
        //IDictionary<char, TrieNode> Children { get; set; }

        /// <summary>
        /// Boolean to indicate whether the root to this node forms a word.
        /// </summary>
        internal bool IsWord
        {
            get { return WordCount > 0; }
        }

        /// <summary>
        /// The count of words for the TrieNode.
        /// </summary>
        internal int WordCount { get; set; }

        #endregion

        #region constructors

        /// <summary>
        /// Create a new TrieNode instance.
        /// </summary>
        /// <param name="character">The character for the TrieNode.</param>
        internal TrieNode(char character)
        {
            Character = character;
            Children = new LinkedList<TrieNode>();
            WordCount = 0;
        }

        #endregion

        #region methods

        /// <summary>
        /// For readability.
        /// </summary>
        /// <returns>Character.</returns>
        public override string ToString()
        {
            return Character.ToString();
        }

        public override bool Equals(object obj)
        {
            TrieNode that;
            return
                obj != null
                && (that = obj as TrieNode) != null
                && that.Character == this.Character;
        }

        public override int GetHashCode()
        {
            return Character.GetHashCode();
        }

        internal LinkedList<TrieNode> GetChildren()
        {
            return Children;
        }

        internal TrieNode GetChild(char character)
        {
            foreach (TrieNode t in Children)
            {
                if (t.Character == character)
                {
                    return t;
                }
            }
            return null;
        }

        internal void SetChild(TrieNode child)
        {
            if (child == null)
            {
                throw new ArgumentNullException(nameof(child));
            }
            Children.AddLast(child);
        }

        internal void RemoveChild(char character)
        {
            foreach (TrieNode t in Children) {
                if (t.Character == character)
                {
                    Children.Remove(t);
                }
            }
        }

        internal void Clear()
        {
            WordCount = 0;
            Children.Clear();
        }

        #endregion
    }
}