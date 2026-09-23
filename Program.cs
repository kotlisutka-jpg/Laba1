using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GeneticSearch
{
    class Program
    {
        struct GeneticData
        {
            public string protein;
            public string organism;
            public string amino_acids;
        }

        struct Command
        {
            public string name;
            public string parameter1;
            public string parameter2;
        }
        static string RLDecoding(string amino_acids)
        {
            if (string.IsNullOrEmpty(amino_acids)) return string.Empty;

            StringBuilder decoded = new StringBuilder();
            for (int i = 0; i < amino_acids.Length; i++)
            {
                char ch = amino_acids[i];
                if (char.IsDigit(ch))
                {
                    int count = ch - '0';
                    char letter = amino_acids[i + 1];

                    for (int j = 0; j < count; j++)
                    {
                        decoded.Append(letter);
                    }
                    i++;
                }
                else
                {
                    decoded.Append(ch);
                }
            }
            return decoded.ToString();
        }
        static string RLEncoding(string amino_acids)
        {
            if (string.IsNullOrEmpty(amino_acids)) return string.Empty;

            StringBuilder encoded = new StringBuilder();
            for (int i = 0; i < amino_acids.Length; i++)
            {
                char ch = amino_acids[i];
                int count = 1;
                while (i < amino_acids.Length - 1 && amino_acids[i + 1] == ch)
                {
                    count++;
                    i++;
                }
                if (count > 2) encoded.Append(count).Append(ch);
                if (count == 1) encoded.Append(ch);
                if (count == 2) encoded.Append(ch).Append(ch);
            }
            return encoded.ToString();
        }

        static List<GeneticData> ReadData(string filename)
        {
            List<GeneticData> data = new List<GeneticData>();

            if (!File.Exists(filename))
            {
                Console.WriteLine($"Ошибка: Файл данных {filename} не найден.");
                return data;
            }

            using (StreamReader reader = new StreamReader(filename))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] parts = line.Split('\t');
                    if (parts.Length >= 3)
                    {
                        GeneticData item;
                        item.protein = parts[0].Trim();
                        item.organism = parts[1].Trim();

                        item.amino_acids = RLDecoding(parts[2].Trim());

                        data.Add(item);
                    }
                }
            }
            return data;
        }

        static void ProcessCommands(string commandsFilename, string outputFilename, List<GeneticData> proteins)
        {
            if (!File.Exists(commandsFilename))
            {
                Console.WriteLine($"Ошибка: Файл команд {commandsFilename} не найден.");
                return;
            }

            using (StreamReader reader = new StreamReader(commandsFilename))
            using (StreamWriter writer = new StreamWriter(outputFilename, false, Encoding.UTF8))
            {
                writer.WriteLine("Ivan Ivanov");
                writer.WriteLine("Genetic Searching");

                int commandCounter = 1;

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] parts = line.Split('\t');
                    Command cmd;
                    cmd.name = parts[0].Trim();
                    cmd.parameter1 = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                    cmd.parameter2 = parts.Length > 2 ? parts[2].Trim() : string.Empty;

                    string idStr = commandCounter.ToString("D3");
                    commandCounter++;

                    writer.WriteLine("--------------------------------------------------------------------------");

                    if (cmd.name == "search")
                    {
                        ExecuteSearch(writer, idStr, cmd.parameter1, proteins);
                    }
                    else if (cmd.name == "diff")
                    {
                        ExecuteDiff(writer, idStr, cmd.parameter1, cmd.parameter2, proteins);
                    }
                    else if (cmd.name == "mode")
                    {
                        ExecuteMode(writer, idStr, cmd.parameter1, proteins);
                    }
                }

                writer.WriteLine("--------------------------------------------------------------------------");
            }
        }

        static void ExecuteSearch(StreamWriter writer, string id, string querySequence, List<GeneticData> proteins)
        {
            writer.WriteLine($"{id}   search   {querySequence} ");
            writer.WriteLine("organism\t\t\t\tprotein ");

            bool foundAny = false;

            foreach (var item in proteins)
            {
                if (item.amino_acids.Contains(querySequence))
                {
                    writer.WriteLine($"{item.organism}\t\t{item.protein}");
                    foundAny = true;
                }
            }

            if (!foundAny)
            {
                writer.WriteLine("NOT FOUND");
            }
        }

        static void ExecuteDiff(StreamWriter writer, string id, string protein1Name, string protein2Name, List<GeneticData> proteins)
        {
            writer.WriteLine($"{id}   diff   {protein1Name}   {protein2Name} ");
            writer.WriteLine("amino-acids difference:");

            var p1 = proteins.FirstOrDefault(p => p.protein.Equals(protein1Name, StringComparison.OrdinalIgnoreCase));
            var p2 = proteins.FirstOrDefault(p => p.protein.Equals(protein2Name, StringComparison.OrdinalIgnoreCase));

            bool missing1 = string.IsNullOrEmpty(p1.protein);
            bool missing2 = string.IsNullOrEmpty(p2.protein);

            if (missing1 || missing2)
            {
                writer.Write("MISSING: ");
                if (missing1 && missing2) writer.WriteLine($"{protein1Name}, {protein2Name}");
                else if (missing1) writer.WriteLine(protein1Name);
                else writer.WriteLine(protein2Name);
                return;
            }

            string s1 = p1.amino_acids;
            string s2 = p2.amino_acids;
            int minLength = Math.Min(s1.Length, s2.Length);
            int differenceCount = 0;

            for (int i = 0; i < minLength; i++)
            {
                if (s1[i] != s2[i])
                {
                    differenceCount++;
                }
            }

            differenceCount += Math.Abs(s1.Length - s2.Length);

            writer.WriteLine(differenceCount);
        }
        static void ExecuteMode(StreamWriter writer, string id, string proteinName, List<GeneticData> proteins)
        {
            writer.WriteLine($"{id}   mode   {proteinName} ");
            writer.WriteLine("amino-acid occurs:");

            var targetProtein = proteins.FirstOrDefault(p => p.protein.Equals(proteinName, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(targetProtein.protein))
            {
                writer.WriteLine($"MISSING: {proteinName}");
                return;
            }

            string sequence = targetProtein.amino_acids;
            if (string.IsNullOrEmpty(sequence))
            {
                writer.WriteLine("Ошибка: последовательность пуста.");
                return;
            }

            var frequencies = new Dictionary<char, int>();
            foreach (char ch in sequence)
            {
                if (!char.IsLetter(ch)) continue;

                if (frequencies.ContainsKey(ch)) frequencies[ch]++;
                else frequencies[ch] = 1;
            }

            var bestAminoAcid = frequencies.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).First();
            writer.WriteLine($"{bestAminoAcid.Key}          {bestAminoAcid.Value}");
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Запуск процесса генетического поиска...");

            string inputSequences = "sequences.txt";
            string inputCommands = "commands.txt";
            string outputFile = "genedata.txt";

            List<GeneticData> database = ReadData(inputSequences);
            Console.WriteLine($"Успешно загружено и расшифровано белков: {database.Count}");

            ProcessCommands(inputCommands, outputFile, database);

            Console.WriteLine($"Обработка завершена. Результаты сохранены в файл: {outputFile}");
        }
    }
}
