namespace BRUNINI_CsvParser.Services
{
	public interface IFileService
	{
		void MoveFileToArchive(string filePath, string archiveFolder);
	}

	public class FileService : IFileService
	{
		public void MoveFileToArchive(string filePath, string archiveFolder)
		{
			string fileName = Path.GetFileName(filePath);
			string destinationPath = Path.Combine(archiveFolder, fileName);
			File.Move(filePath, destinationPath);
		}
	}
}


