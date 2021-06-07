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
        private Dictionary<int, List<int>> adjacentDictionary;
        
        #endregion

        #region Constructor

        public PINCracker()
        {
            checkAdjacentNumbers = true;
            considerSequence = true;

            adjacentDictionary = new Dictionary<int, List<int>>
            {
                {1, new List<int> {1, 2, 4}},
                {2, new List<int> {1, 2, 3, 5}},
                {3, new List<int> {2, 3, 6}},
                {4, new List<int> {1, 4, 5, 7}},
                {5, new List<int> {2, 4, 5, 6, 8}},
                {6, new List<int> {3, 5, 6, 9}},
                {7, new List<int> {4, 7, 8}},
                {8, new List<int> {5, 7, 8, 9, 0}},
                {9, new List<int> {6, 8, 9,}},
                {0, new List<int> {8, 0}}
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
        public Dictionary<int, List<int>> AdjacentDictionary
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
            var startTick = Environment.TickCount64;
            var possibleNumbers = GetAdjacentNumbersFrom((int)char.GetNumericValue(pin[pinIndex]));
            var queue = new  Queue<KeyValuePair<long,long>>(possibleNumbers.Select(x => new KeyValuePair<long, long>(0,x)));
            
            while (queue.Any())
            {
                pinIndex = (int)GetPINLength(queue.Peek());
                if (pinIndex < pin.Length)
                {
                    var value = queue.Dequeue();
                    foreach (var number in GetAdjacentNumbersFrom((int)char.GetNumericValue(pin[pinIndex])))
                    {
                        var leadingZeroCounter = value.Key;
                        if (number == 0 && value.Value == 0)
                            leadingZeroCounter++;
                        var kvp = new KeyValuePair<long, long>(leadingZeroCounter, value.Value * 10 + number);
                        queue.Enqueue(kvp);
                    }
                }
                else
                {
                    var returnValue = queue.Dequeue().Value.ToString().PadLeft(pin.Length, '0');
                    yield return returnValue;
                }
            }
            var duration = new TimeSpan(Environment.TickCount64 - startTick);
            OnGotOutput($"Finished - operation took {duration:c}");
        }

        /// <summary>
        /// Returns ALL possible PINs, basically brute-forcing every combination from a specified subset
        /// </summary>
        /// <param name="pin">The PIN to crack</param>
        /// <returns></returns>
        private IEnumerable<string> GetAllPossiblePINs(string pin)
        {
            var startTick = Environment.TickCount64;
            var possibleNumbers = new HashSet<long>();
            possibleNumbers.UnionWith(GetPINNumbers(pin));
            if (checkAdjacentNumbers)
            {
                var adjacentNumbers = possibleNumbers.SelectMany(n => GetAdjacentNumbersFrom((int)n)).ToList();
                foreach (var adjacent in adjacentNumbers)
                    possibleNumbers.Add(adjacent);
            }

            var possibleCombinationsCount = Math.Pow(possibleNumbers.Count, pin.Length);
            OnGotOutput($"Getting all {possibleCombinationsCount} possible PIN Combinations\n" +
                        $"(The sequence of the PINs numbers is irrelevant => ALL combinations)\n");

            var queue = new Queue<KeyValuePair<long,long>>(possibleNumbers.Select(x => new KeyValuePair<long, long>(0,x)));
            while (queue.Any())
            {
                if (GetPINLength(queue.Peek()) < pin.Length)
                {
                    var value = queue.Dequeue();
                    foreach (var number in possibleNumbers)
                    {
                        var leadingZeroCounter = value.Key;
                        if (number == 0 && value.Value == 0)
                            leadingZeroCounter++;

                        var kvp = new KeyValuePair<long, long>(leadingZeroCounter, value.Value * 10 + number);
                        queue.Enqueue(kvp);
                    }
                }
                else
                {
                    var returnValue = queue.Dequeue().Value.ToString().PadLeft(pin.Length, '0');
                    yield return returnValue;
                }
                   
            }

            var duration = new TimeSpan(Environment.TickCount64 - startTick);
            OnGotOutput($"Finished - operation took {duration:c}");
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

        private long GetPINLength(KeyValuePair<long, long> kvp)
        {
            var pinLength = 1;
            var value = kvp.Value;
            while (value / 10 > 0)
            {
                value /= 10;
                pinLength++;
            }
            return pinLength + kvp.Key;
        }
        
        private static IEnumerable<long> GetPINNumbers(string pin) =>
            pin.Select(p => (long)char.GetNumericValue(p));
        
        private IEnumerable<long> GetAdjacentNumbersFrom(int possibleNumber)
        {
            if (!adjacentDictionary.ContainsKey(possibleNumber)) throw new InvalidOperationException("The specified number was not in Adjacent-Dictionary!");
            foreach (var number in adjacentDictionary[possibleNumber])
                yield return number;
        }
        
        #endregion
    }
}