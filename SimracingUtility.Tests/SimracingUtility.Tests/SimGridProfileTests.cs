using SimracingUtility.Services;

namespace SimracingUtility.Tests
{
    public class SimGridProfileTests
    {
        [Theory]
        [InlineData("https://www.thesimgrid.com/drivers/8444-ranokar/activities")]
        [InlineData("https://www.thesimgrid.com/drivers/8444-ranokar")]
        [InlineData("https://thesimgrid.com/drivers/8444-ranokar")]
        [InlineData("www.thesimgrid.com/drivers/8444-ranokar")]
        [InlineData("thesimgrid.com/drivers/8444-ranokar/activities")]
        [InlineData("https://www.thesimgrid.com/drivers/8444-ranokar/")]
        [InlineData("https://www.thesimgrid.com/drivers/8444-ranokar/activities?tab=results")]
        public void TryParse_ValidUrls_ExtractsSlugAndId(string input)
        {
            var ok = SimGridProfile.TryParse(input, out var slug, out var id, out var url);

            Assert.True(ok);
            Assert.Equal("8444-ranokar", slug);
            Assert.Equal(8444, id);
            Assert.Equal("https://www.thesimgrid.com/drivers/8444-ranokar", url);
        }

        [Fact]
        public void TryParse_BareSlug_IsAccepted()
        {
            var ok = SimGridProfile.TryParse("8444-ranokar", out var slug, out var id, out var url);

            Assert.True(ok);
            Assert.Equal("8444-ranokar", slug);
            Assert.Equal(8444, id);
            Assert.Equal("https://www.thesimgrid.com/drivers/8444-ranokar", url);
        }

        [Fact]
        public void TryParse_BareId_IsAccepted()
        {
            var ok = SimGridProfile.TryParse("8444", out var slug, out var id, out var url);

            Assert.True(ok);
            Assert.Equal("8444", slug);
            Assert.Equal(8444, id);
            Assert.Equal("https://www.thesimgrid.com/drivers/8444", url);
        }

        [Fact]
        public void TryParse_MultiPartName_KeepsFullSlug()
        {
            var ok = SimGridProfile.TryParse("https://www.thesimgrid.com/drivers/1234-john-doe", out var slug, out var id, out _);

            Assert.True(ok);
            Assert.Equal("1234-john-doe", slug);
            Assert.Equal(1234, id);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("hello")]
        [InlineData("ranokar")]                                              // keine fuehrende Id
        [InlineData("8444-ranokar extra")]                                   // Junk im Slug
        [InlineData("https://evil.com/drivers/8444-ranokar")]                // falscher Host
        [InlineData("https://www.thesimgrid.com/championships/123")]         // kein /drivers
        [InlineData("https://www.thesimgrid.com/drivers/")]                  // kein Slug
        [InlineData("https://www.thesimgrid.com/drivers/abc")]               // keine Id
        public void TryParse_InvalidInputs_AreRejected(string? input)
        {
            var ok = SimGridProfile.TryParse(input, out var slug, out var id, out var url);

            Assert.False(ok);
            Assert.Equal(string.Empty, slug);
            Assert.Equal(0, id);
            Assert.Equal(string.Empty, url);
        }
    }
}
