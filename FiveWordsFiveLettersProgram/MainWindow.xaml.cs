using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FiveWordsFiveLettersProgram
{
    public partial class MainWindow : Window
    {
        private int validCombinationCount = 0; // Count of valid combinations

        public MainWindow()
        {
            InitializeComponent();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the selected file
            string selectedFile = GetSelectedFile();
            if (string.IsNullOrEmpty(selectedFile))
            {
                MessageBox.Show("Please select a valid file.");
                return;
            }

            // Reset valid combination count and progress bar
            validCombinationCount = 0;
            ResultsText.Text = "Results will appear here.";
            Progress.Value = 0;

            // Start processing words
            ProcessWords(selectedFile);
        }

        private string GetSelectedFile()
        {
            if (FileComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                switch (selectedItem.Content.ToString())
                {
                    case "TestData":
                        Progress.Maximum = 1;
                        return "TestData.txt";
                    case "BetaData":
                        Progress.Maximum = 77;
                        return "BetaData.txt";
                    case "AlphaData":
                        Progress.Maximum = 831;
                        return "AlphaData.txt";
                    default:
                        return null;
                }
            }
            return null;
        }

        private async void ProcessWords(string filePath)
        {
            // Read words from the file and filter them
            List<(string word, int bitmask)> words = LoadAndFilterWords(filePath);

            if (words.Count == 0)
            {
                ResultsText.Text = "No valid 5-letter words found.";
                return;
            }

            // Calculate total combinations
            int totalCombinations = CalculateTotalCombinations(words.Count);
           // Progress.Maximum = totalCombinations;
            Progress.Value = 0;

            // Debug output for total combinations
            MessageBox.Show($"Total combinations to check: {totalCombinations}");

            // Find combinations with exactly 25 unique letters
            List<string[]> validCombinations = await Task.Run(() => FindCombinationsWithUniqueLetters(words));

            // Update the UI with results
            ResultsText.Text = $"Found {validCombinations.Count}";
            Progress.Value = validCombinations.Count;
        }

        private int CalculateTotalCombinations(int wordCount)
        {
            // Calculate the total number of combinations that could possibly be checked
            if (wordCount < 5) return 0; // Not enough words to form combinations
            return wordCount * (wordCount - 1) * (wordCount - 2) * (wordCount - 3) * (wordCount - 4) / 120; // Combination nC5
        }

        private List<(string word, int bitmask)> LoadAndFilterWords(string filePath)
        {
            try
            {
                var wordsList = File.ReadLines(filePath)
                    .Select(word => word.Trim().ToLower())
                    .Where(word => word.Length == 5) // Only 5-letter words
                    .Select(word => (word, GetBitmask(word)))
                    .Where(tuple => tuple.Item2 != 0) // Only words with unique letters
                    .ToList();

                // Debug output
                MessageBox.Show($"Loaded {wordsList.Count} valid 5-letter words from {filePath}.");
                return wordsList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading file: {ex.Message}");
                return new List<(string word, int bitmask)>();
            }
        }

        private async Task<List<string[]>> FindCombinationsWithUniqueLetters(List<(string word, int bitmask)> words)
        {
            int wordCount = words.Count;
            List<string[]> validCombinations = new List<string[]>();
            object lockObject = new object();

            // Parallel processing to find combinations
            await Task.Run(() =>
            {
                Parallel.For(0, wordCount, i =>
                {
                    for (int j = i + 1; j < wordCount; j++)
                    {
                        if ((words[i].bitmask & words[j].bitmask) != 0) continue;

                        int bitmaskIJ = words[i].bitmask | words[j].bitmask;

                        for (int k = j + 1; k < wordCount; k++)
                        {
                            if ((bitmaskIJ & words[k].bitmask) != 0) continue;

                            int bitmaskIJK = bitmaskIJ | words[k].bitmask;

                            for (int l = k + 1; l < wordCount; l++)
                            {
                                if ((bitmaskIJK & words[l].bitmask) != 0) continue;

                                int bitmaskIJKL = bitmaskIJK | words[l].bitmask;

                                for (int m = l + 1; m < wordCount; m++)
                                {
                                    if ((bitmaskIJKL & words[m].bitmask) != 0) continue;

                                    int combinedBitmask = bitmaskIJKL | words[m].bitmask;

                                    if (CountSetBits(combinedBitmask) == 25) // Check for exactly 25 unique letters
                                    {
                                        lock (lockObject)
                                        {
                                            validCombinations.Add(new[] { words[i].word, words[j].word, words[k].word, words[l].word, words[m].word });
                                            validCombinationCount++; // Increment valid combination count
                                            Application.Current.Dispatcher.Invoke(() =>
                                            {
                                                Progress.Value++; // Update progress for each valid combination
                                                ResultsText.Text = $"Found {validCombinationCount} combinations so far.";
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                });
            });

            return validCombinations;
        }

        private int GetBitmask(string word)
        {
            int bitmask = 0;
            foreach (char c in word)
            {
                int bit = 1 << (c - 'a');
                if ((bitmask & bit) != 0)
                    return 0; // Word has duplicate letters
                bitmask |= bit;
            }
            return bitmask;
        }

        private int CountSetBits(int n)
        {
            int count = 0;
            while (n > 0)
            {
                count += n & 1;
                n >>= 1;
            }
            return count;
        }
    }
}
