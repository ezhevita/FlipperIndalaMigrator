using System.Collections;

Console.WriteLine("Enter path to RFID cards:");
var path = Console.ReadLine();
while (string.IsNullOrEmpty(path) || !Directory.Exists(path))
{
	Console.WriteLine("Path is invalid, try again:");
	path = Console.ReadLine();
}

var files = Directory.EnumerateFiles(path, "*.rfid");

var checksumBits = new byte[] {0, 2, 5, 6, 8, 9, 12, 14};
foreach (var file in files)
{
	var fileLines = await File.ReadAllLinesAsync(file);
	const string KeyTypePrefix = "Key type: ";
	var keyTypeLine = fileLines.FirstOrDefault(x => x.StartsWith(KeyTypePrefix));

	if (keyTypeLine == null || keyTypeLine[KeyTypePrefix.Length..] != "I40134")
		continue;

	const string DataPrefix = "Data: ";
	var dataLine = fileLines.FirstOrDefault(x => x.StartsWith(DataPrefix));
	var dataIndex = Array.IndexOf(fileLines, dataLine);

	if (dataLine == null)
		continue;

	var data = dataLine[DataPrefix.Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

	if (data.Count >= 4)
		continue;

	Console.WriteLine($"Processing {file}...");
	var bitArray = new BitArray(data.Select(x => Convert.ToByte(x, 16)).Reverse().ToArray());

	var checksum = checksumBits.Aggregate(0, (current, checksumBit) => current + (bitArray[checksumBit] ? 1 : 0));
	data.Add((checksum & 1) == 1 ? "01" : "02");
	fileLines[dataIndex] = DataPrefix + string.Join(' ', data);
	await File.WriteAllTextAsync(file, string.Join('\n', fileLines));
	Console.WriteLine($"Processed {file}");
}