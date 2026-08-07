namespace EvilHop.Tests
{
    public class PlaceholderTests
    {
        [Fact]
        public void Placeholder_ExposesLibraryName()
        {
            Assert.Equal("EvilHop", Placeholder.Name);
        }
    }
}
