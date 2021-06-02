using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PINCrack
{
    public class PINCracker
    {
        #region member fields

        private bool checkAdjacentNumbers;
        private bool considerSequence;
        private Dictionary<string, List<string>> adjacentDictionary;
        
        #endregion

        #region Constructor

        public PINCracker()
        {
            checkAdjacentNumbers = true;
            considerSequence = true;

            adjacentDictionary =  new Dictionary<string, List<string>>()
            {
                {"1", new List<string> { "1", "2", "4" } },
                {"2", new List<string> { "1", "2", "3", "5" }},
                {"3", new List<string> { "2", "3", "6" }},
                {"4", new List<string> { "1", "4", "5", "7" }},
                {"5", new List<string> { "2", "4", "5", "6", "8" }},
                {"6", new List<string> { "3", "5", "6", "9" }},
                {"7", new List<string> { "4", "7", "8" }},
                {"8", new List<string> { "5", "7", "8", "9", "0" }},
                {"9", new List<string> { "6", "8", "9", }},
                {"0", new List<string> { "8", "0" }}
            };
        }
        
        #endregion
        
        #region Properties

        /// <summary>
        /// Whether to check the neighbouring/adjacent number
        /// </summary>
        public bool CheckAdjacentNumbers
        {
            get => checkAdjacentNumbers;
            set => checkAdjacentNumbers = value;
        }

        /// <summary>
        /// Whether each entered digits sequence should be valued
        /// </summary>
        public bool ConsiderSequence
        {
            get => considerSequence;
            set => considerSequence = value;
        }

        /// <summary>
        /// Maps each digit to the corresponding adjacent digits
        /// </summary>
        public Dictionary<string, List<string>> AdjacentDictionary
        {
            set => adjacentDictionary = value;
        }

        /// <summary>
        /// Broadcasts the output with  this event
        /// </summary>
        public EventHandler<string> OutputEvent;

        #endregion

        #region Methods

        /// <summary>
        /// Returns the possible combinations for the specified PIN
        /// </summary>
        /// <param name="pin">The PIN to crack</param>
        /// <returns></returns>
        public IEnumerable<string> CrackPin(string pin) =>
             considerSequence ? GetPossiblePINs(pin) : GetAllPossiblePINs(pin);

        /// <summary>
        /// Returns the possible PINs, respecting the order of the PINs digits.
        /// </summary>
        /// <param name="pin">The PIN to crack</param>
        /// <returns></returns>
        private IEnumerable<string> GetPossiblePINs(string pin)
        {
            var pinIndex = 0;
            var possibleNumbers = GetAdjacentNumbersFrom(pin[pinIndex].ToString());
            var queue = new Queue<string>(possibleNumbers);
            while (queue.Any())
            {
                if (queue.Peek().Length < pin.Length)
                {
                    var value = queue.Dequeue();
                    pinIndex = value.Length;
                    foreach (var number in GetAdjacentNumbersFrom(pin[pinIndex].ToString()))
                        queue.Enqueue(value + number);
                }
                else
                    yield return queue.Dequeue();
            }
        }

        /// <summary>
        /// Returns ALL possible PINs, basically brute-forcing every combination from a specified subset
        /// </summary>
        /// <param name="pin">The PIN to crack</param>
        /// <returns></returns>
        private IEnumerable<string> GetAllPossiblePINs(string pin)
        {
            var possibleNumbers = new HashSet<string>();
            possibleNumbers.UnionWith(GetPINNumbers(pin));
            if (checkAdjacentNumbers)
            {
                var adjacentNumbers = possibleNumbers.SelectMany(GetAdjacentNumbersFrom).ToList();
                foreach (var adjacent in adjacentNumbers)
                    possibleNumbers.Add(adjacent);
            }

            var possibleCombinationsCount = Math.Pow(possibleNumbers.Count, pin.Length);
            OnGotOutput($"Getting all {possibleCombinationsCount} possible PIN Combinations\n" +
                          $"(The sequence of the PINs numbers is irrelevant => ALL combinations)\n" +
                          $"---Depending on the length of the PIN, this might consume A LOT RAM---");

            var queue = new Queue<string>(possibleNumbers.Select(x => x.ToString()));
            while (queue.Any())
            {
                if (queue.Peek().Length < pin.Length)
                {
                    var value = queue.Dequeue();
                    foreach (var number in possibleNumbers)
                        queue.Enqueue(value + number);
                }
                else
                    yield return queue.Dequeue();
            }
        }

        #endregion

        #region Events

        private void OnGotOutput(string e)
        {
            var handler = OutputEvent;
            handler?.Invoke(this, e);
        }

        #endregion

        #region Helper Methods

        private static IEnumerable<string> GetPINNumbers(string pin) =>
            pin.Select(p => char.GetNumericValue(p).ToString(CultureInfo.InvariantCulture));
        
        /// <summary>
        /// Get all adjacent numbers from an single number (of type <see cref="string"/>)
        /// </summary>
        /// <param name="possibleNumber">The number to get the adjacent numbers from</param>
        /// <returns></returns>
        private IEnumerable<string> GetAdjacentNumbersFrom(string possibleNumber)
        {
            if (!adjacentDictionary.ContainsKey(possibleNumber)) throw new InvalidOperationException("The specified number was not in Adjacent-Dictionary!");
            foreach (var number in adjacentDictionary[possibleNumber])
                yield return number;
        }

        #endregion
    }
}