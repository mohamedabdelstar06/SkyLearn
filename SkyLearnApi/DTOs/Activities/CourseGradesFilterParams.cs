namespace SkyLearnApi.DTOs.Activities
{
    public class CourseGradesFilterParams
    {
        /// <summary>Filter by student name or ID (partial match on name).</summary>
        public string? StudentSearch { get; set; }

        /// <summary>Filter items by type: "Quiz", "Assignment", or "Lecture".</summary>
        public string? ItemType { get; set; }

        // Pagination
        private int _pageNumber = 1;
        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        private int _pageSize = 20;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > 100 ? 100 : value < 1 ? 1 : value;
        }
    }
}
