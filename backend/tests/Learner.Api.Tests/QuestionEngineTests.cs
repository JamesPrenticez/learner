using System.Text.Json;
using Learner.Api.Features;

namespace Learner.Api.Tests;

/// <summary>Pure logic, no HTTP: question grading and question selection.</summary>
public class QuestionEngineTests
{
    static JsonElement J(string s) => JsonDocument.Parse(s).RootElement;

    [Theory]
    [InlineData("single", """{"options":[{"id":"a","text":"A"},{"id":"b","text":"B"}],"correct":"b"}""", """{"choice":"b"}""", 1)]
    [InlineData("single", """{"options":[{"id":"a","text":"A"},{"id":"b","text":"B"}],"correct":"b"}""", """{"choice":"a"}""", 0)]
    [InlineData("single", """{"options":[{"id":"a","text":"A"},{"id":"b","text":"B"}],"correct":"b"}""", """{}""", 0)]
    [InlineData("multiple", """{"options":[{"id":"a","text":"A"},{"id":"b","text":"B"},{"id":"c","text":"C"}],"correct":["a","c"]}""", """{"choices":["c","a"]}""", 1)]
    [InlineData("multiple", """{"options":[{"id":"a","text":"A"},{"id":"b","text":"B"},{"id":"c","text":"C"}],"correct":["a","c"]}""", """{"choices":["a"]}""", 0)]
    [InlineData("multiple", """{"options":[{"id":"a","text":"A"},{"id":"b","text":"B"},{"id":"c","text":"C"}],"correct":["a","c"]}""", """{"choices":["a","b","c"]}""", 0)]
    [InlineData("truefalse", """{"correct":true}""", """{"value":true}""", 1)]
    [InlineData("truefalse", """{"correct":true}""", """{"value":false}""", 0)]
    [InlineData("truefalse", """{"correct":false}""", """{"value":"false"}""", 0)]
    [InlineData("fillblank", """{"accepted":["Practical Completion","PC"]}""", """{"text":"  practical   completion "}""", 1)]
    [InlineData("fillblank", """{"accepted":["Practical Completion","PC"]}""", """{"text":"pc"}""", 1)]
    [InlineData("fillblank", """{"accepted":["PC"],"caseSensitive":true}""", """{"text":"pc"}""", 0)]
    [InlineData("fillblank", """{"accepted":["PC"]}""", """{"text":""}""", 0)]
    [InlineData("numeric", """{"answer":12.5,"tolerance":0.1}""", """{"value":12.45}""", 1)]
    [InlineData("numeric", """{"answer":12.5,"tolerance":0.1}""", """{"value":"12.6"}""", 1)]
    [InlineData("numeric", """{"answer":12.5,"tolerance":0.1}""", """{"value":13}""", 0)]
    [InlineData("numeric", """{"answer":10}""", """{"value":10}""", 1)]
    [InlineData("numeric", """{"answer":10}""", """{"value":"abc"}""", 0)]
    public void Grading(string type, string data, string response, double expected) =>
        Assert.Equal(expected, QuestionTypes.All[type].Grade(J(data), J(response)));

    [Theory]
    [InlineData("single", """{"options":[{"id":"a","text":"A"}],"correct":"a"}""")] // one option
    [InlineData("single", """{"options":[{"id":"a","text":"A"},{"id":"b","text":"B"}],"correct":"z"}""")] // key not an option
    [InlineData("single", """{"options":[{"id":"a","text":"A"},{"id":"a","text":"B"}],"correct":"a"}""")] // duplicate ids
    [InlineData("multiple", """{"options":[{"id":"a","text":"A"},{"id":"b","text":"B"}],"correct":[]}""")]
    [InlineData("truefalse", """{"correct":"yes"}""")]
    [InlineData("fillblank", """{"accepted":[" "]}""")]
    [InlineData("numeric", """{"answer":"ten"}""")]
    [InlineData("numeric", """{"answer":1,"tolerance":-1}""")]
    public void Invalid_questions_are_rejected(string type, string data) => Assert.NotNull(QuestionTypes.All[type].Validate(J(data)));

    [Fact]
    public void Learners_never_receive_the_answer_key()
    {
        var node = QuestionTypes.ForLearner(QuestionTypes.All["numeric"], """{"answer":5,"tolerance":1}""");
        Assert.Empty(node.AsObject());
        var single = QuestionTypes.ForLearner(QuestionTypes.All["single"], """{"options":[{"id":"a","text":"A"}],"correct":"a"}""").AsObject();
        Assert.False(single.ContainsKey("correct"));
        Assert.True(single.ContainsKey("options"));
    }

    // ---- selection

    [Fact]
    public void Never_opens_with_the_question_just_seen_when_there_is_an_alternative()
    {
        for (var seed = 0; seed < 500; seed++)
        {
            var picked = QuestionSelector.Pick([1, 2, 3], 3, [2], shuffle: true, new Random(seed));
            Assert.NotEqual(2, picked[0]);
            Assert.Equal([1, 2, 3], picked.Order());
        }
    }

    [Fact]
    public void Prefers_questions_not_seen_recently()
    {
        for (var seed = 0; seed < 100; seed++)
            Assert.Equal([4, 5, 6], QuestionSelector.Pick([1, 2, 3, 4, 5, 6], 3, [1, 2, 3], true, new Random(seed)).Order());
    }

    [Fact]
    public void When_everything_was_seen_it_picks_the_least_recent()
    {
        for (var seed = 0; seed < 50; seed++)
            Assert.Equal([3], QuestionSelector.Pick([1, 2, 3], 1, [1, 2, 3], true, new Random(seed)));
    }

    [Fact]
    public void A_single_question_pool_reuses_it_instead_of_blocking()
    {
        Assert.Equal([7], QuestionSelector.Pick([7], 1, [7], true, new Random(1)));
        Assert.Equal([7], QuestionSelector.Pick([7], 5, [7], true, new Random(1))); // count is capped at the pool size
    }

    [Fact]
    public void Authored_order_is_kept_when_not_shuffling_and_there_is_no_history()
    {
        Assert.Equal([1, 2, 3, 4], QuestionSelector.Pick([1, 2, 3, 4], 4, [], shuffle: false, new Random(1)));
        Assert.Empty(QuestionSelector.Pick([], 3, [], true, new Random(1)));
    }
}
