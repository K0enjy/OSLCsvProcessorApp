using System;
using System.Collections.Generic;
using System.IO;

public class FileService : FileService.IFileService
{
	public interface IFileService
	{
		IEnumerable<string> GetCsvFiles(string folderPath);
		void MoveFileToArchive(string filePath, string archiveFolder);
	}

	public IEnumerable<string> GetCsvFiles(string folderPath)
	{
		return Directory.GetFiles(folderPath, "*.csv");
	}

	public void MoveFileToArchive(string filePath, string archiveFolder)
	{
		string fileName = Path.GetFileName(filePath);
		string destinationPath = Path.Combine(archiveFolder, fileName);
		File.Move(filePath, destinationPath);
	}
}
