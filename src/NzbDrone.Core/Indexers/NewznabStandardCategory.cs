using System.Collections.Generic;
using System.Linq;

namespace NzbDrone.Core.Indexers
{
    public static class NewznabStandardCategory
    {
        public static readonly IndexerCategory Books = new (7000, "Books");
        public static readonly IndexerCategory BooksManga = new (7070, "Books/Manga");
        public static readonly IndexerCategory BooksManhua = new (7080, "Books/Manhua");
        public static readonly IndexerCategory BooksManhwa = new (7090, "Books/Manhwa");
        public static readonly IndexerCategory Other = new (8000, "Other");

        public static readonly IndexerCategory[] ParentCats =
        {
            Books,
            Other
        };

        public static readonly IndexerCategory[] AllCats =
        {
            Books,
            BooksManga,
            BooksManhua,
            BooksManhwa,
            Other
        };

        static NewznabStandardCategory()
        {
            Books.SubCategories.AddRange(
                new List<IndexerCategory>
                {
                    BooksManga,
                    BooksManhua,
                    BooksManhwa
                });
        }

        public static string GetCatDesc(int torznabCatId) =>
            AllCats.FirstOrDefault(c => c.Id == torznabCatId)?.Name ?? string.Empty;

        public static IndexerCategory GetCatByName(string name) => AllCats.FirstOrDefault(c => c.Name == name);
    }
}
