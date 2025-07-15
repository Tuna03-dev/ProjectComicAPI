using DotNetTruyen.Models;
using DotNetTruyen.ViewModels.Management;

namespace DotNetTruyen.ViewModels
{
	public class ReadHistoryViewModel
	{
		public List<ReadHistory> ReadHistories { get; set; } = default!;
		public int CurrentPage { get; set; }
		public int TotalPages { get; set; }
	}
}
