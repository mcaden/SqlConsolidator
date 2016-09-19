namespace SqlConsolidator
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    class Program
    {
        const string InsertDelimiter = ") VALUES (";

        static void Main(string[] args)
        {
            IEnumerable<string> files;
            if (args.Any())
            {
                files = args.Select(path => path.Trim(new[] { ' ', '"' }));
            }
            else
            {
                Console.WriteLine("Give a comma-delimited list of files to consolidate: ");
                var input = Console.ReadLine();
                files = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(path => path.Trim(new[] { ' ', '"' }));
            }

            try
            {
                foreach (var filePath in files)
                {
                    int counter = 0;
                    string line;
                    string lastInsertData = null;
                    StringBuilder insert = null;

                    // Read the file and output it line by line.
                    using (StreamReader file = new StreamReader(filePath))
                    using (StreamWriter output = new StreamWriter($"{filePath}.consolidated.sql", false))
                    {
                        Action WriteInsertBatch = () =>
                        {
                            if (insert != null)
                            {
                                output.WriteLine(insert.ToString());
                                insert = null;
                                lastInsertData = null;
                                counter = 0;
                            }
                        };

                        while ((line = file.ReadLine()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(line) || line.Trim().Equals("GO"))
                            {
                                continue;
                            }

                            if (line.Contains(InsertDelimiter))
                            {
                                var lineData = line.Split(new[] { InsertDelimiter }, StringSplitOptions.None);
                                if (insert != null && (counter >= 990 || lineData[0] != lastInsertData))
                                {
                                    WriteInsertBatch();
                                }

                                if (insert == null)
                                {
                                    counter = 1;
                                    insert = new StringBuilder();
                                    lastInsertData = lineData[0];
                                    insert.Append(lastInsertData);
                                    insert.Append(InsertDelimiter);
                                    insert.Append(lineData[1]);
                                }
                                else
                                {
                                    insert.Append($", ({lineData[1]}");
                                    counter++;
                                }
                            }
                            else
                            {
                                if (insert != null && !line.StartsWith("SET"))
                                {
                                    insert.Append(line);
                                    continue;
                                }

                                WriteInsertBatch();
                                output.WriteLine(line);
                            }
                        }

                        WriteInsertBatch();
                    }
                }
                Console.Clear();
                Console.WriteLine("Succeeded");
            }
            catch (Exception exc)
            {
                Console.Clear();
                Console.WriteLine(exc.ToString());
            }

            Console.WriteLine(string.Empty);
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}