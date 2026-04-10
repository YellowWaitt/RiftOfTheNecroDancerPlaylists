namespace RiftOfTheNecroDancerPlaylists.UnitTests;

public class NameParserTests
{
    private static readonly string[] SamplesNames = [
        "Tsukasa Saitoh",
        "Casey Edwards ft. Victor Borba",
        "C. Larkin",
        "Christopher Larkin",
        "Danny Baranowsky (feat. FamilyJules)",
        "d. Baranowsky(feat. FamilyJules)",
        "    d.    Baranowsky\t(\nfeat.        FamilyJules )  ",
        "SpellingPhailer, Nirre, Acid Usag​i",
        "Stavros Markonis (feat. Chris Christodoulou & Orestis Zafiriou)",
        "Lord X",
        "Lord X ft. Redglove, Deutsch",
        "Majin ft. Boundless A. Games",
        "A. Dupont",
        "Arthur Dupont",
        "FFXX, Im Baek Hun",
        "YOASOBI / Aimer",
        "Dr.Viossy",
        "Dr. Viossy",
        "P.T.Adamczyk",
        "P.T. Adamczyk",
        " ",
        ""
    ];

    private static readonly HashSet<string>[] ExceptedMatches = [
        ["tsukasa saitoh"],
        ["casey edwards", "victor borba"],
        ["christopher larkin"],
        ["christopher larkin"],
        ["danny baranowsky", "familyjules"],
        ["danny baranowsky", "familyjules"],
        ["danny baranowsky", "familyjules"],
        ["spellingphailer", "nirre", "acid usag​i"],
        ["stavros markonis", "chris christodoulou", "orestis zafiriou"],
        ["lord x"],
        ["lord x", "redglove", "deutsch"],
        ["majin", "boundless a. games"],
        ["arthur dupont"],
        ["arthur dupont"],
        ["ffxx", "im baek hun"],
        ["yoasobi", "aimer"],
        ["dr. viossy"],
        ["dr. viossy"],
        ["p.t. adamczyk"],
        ["p.t. adamczyk"],
        [""],
        [""]
    ];

    [Fact]
    public void GetMatches_OnSampleNames_GivesExceptedMatches()
    {
        var nameParser = new NameParser();
        foreach (var name in SamplesNames)
        {
            nameParser.ParseName(name);
        }
        nameParser.MatchPendingAbbreviations();

        foreach (var (name, excepted) in SamplesNames.Zip(ExceptedMatches))
        {
            Assert.Equal(excepted, nameParser.GetMatches(name));
        }
    }
}
