using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Learner.Api.Data;

public static class Seed
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDb>();

        // ponytail: EnsureCreated, no migrations. Delete learner.db after a schema change; add EF migrations once data matters.
        await db.Database.EnsureCreatedAsync();

        var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles.All)
            if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole(role));

        if (!sp.GetRequiredService<IConfiguration>().GetValue<bool>("Seed:Demo")) return;

        var users = sp.GetRequiredService<UserManager<AppUser>>();
        await EnsureUser(users, "admin@example.com", "Demo Admin", Roles.Admin);
        var teacher = await EnsureUser(users, "teacher@example.com", "Demo Teacher", Roles.Teacher);
        await EnsureUser(users, "learner@example.com", "Demo Learner", Roles.Learner);
        await DemoCourse(db, teacher);
    }

    // Demo accounts only exist when Seed:Demo is true (appsettings.Development.json).
    static async Task<AppUser> EnsureUser(UserManager<AppUser> users, string email, string name, string role)
    {
        if (await users.FindByEmailAsync(email) is { } existing) return existing;
        var user = new AppUser { UserName = email, Email = email, DisplayName = name, EmailConfirmed = true };
        await users.CreateAsync(user, "Passw0rd!");
        await users.AddToRoleAsync(user, role);
        return user;
    }

    static string Json(object o) => JsonSerializer.Serialize(o);

    static Question Single(string text, string[] options, int correct, string explanation, params Competency[] comps) => new()
    {
        Type = "single",
        Text = text,
        Explanation = explanation,
        Competencies = [.. comps],
        DataJson = Json(new { options = options.Select((o, i) => new { id = ((char)('a' + i)).ToString(), text = o }), correct = ((char)('a' + correct)).ToString() }),
    };

    /// <summary>
    /// Sample content so a fresh checkout has something to try. It is ordinary course data created through the same
    /// tables a teacher uses; the platform has no knowledge of it.
    /// </summary>
    static async Task DemoCourse(AppDb db, AppUser teacher)
    {
        if (await db.Courses.AnyAsync()) return;

        Competency Comp(string name, string description) => new() { Name = name, Description = description };
        var interpretation = Comp("Contract interpretation", "Read a contract and identify what it requires.");
        var variations = Comp("Variation assessment", "Recognise and assess variations to the works.");
        var payment = Comp("Payment claims", "Understand progress claims and payment obligations.");

        var bank = new QuestionBank
        {
            Name = "Fundamentals",
            Questions =
            [
                Single("Which best describes a variation?", ["A change to the scope of the works that is directed or agreed under the contract", "Any delay to the works", "The final payment on completion", "A defect found after handover"], 0,
                    "A variation changes the scope of the works. How it must be instructed and valued is set by the contract.", variations),
                new Question
                {
                    Type = "truefalse", Text = "Most construction contracts set out a required process, such as a written direction, for instructing variations.",
                    Explanation = "Following the contract's variation procedure is usually essential to being paid for the extra work.", Competencies = [variations],
                    DataJson = Json(new { correct = true }),
                },
                new Question
                {
                    Type = "numeric", Text = "A subcontract sum is $200,000 and 5% retention is held. How many dollars are held as retention?", Marks = 2,
                    Explanation = "5% of $200,000 is $10,000.", Hint = "Multiply the sum by 0.05.", Competencies = [payment], Difficulty = Difficulty.Beginner,
                    DataJson = Json(new { answer = 10000, tolerance = 0 }),
                },
                new Question
                {
                    Type = "fillblank", Text = "Security of ______ legislation regulates progress payments in the Australian construction industry.",
                    Explanation = "Security of payment legislation gives contractors a statutory path to progress payments.", Competencies = [payment, interpretation],
                    DataJson = Json(new { accepted = new[] { "payment" }, caseSensitive = false }),
                },
            ],
        };

        var course = new Course
        {
            Title = "Contract Administration Fundamentals",
            Description = "Learn how to read a construction contract and administer variations and payments, with short quizzes and a hands-on contract review.",
            Owner = teacher,
            Visibility = Visibility.Public,
            Status = CourseStatus.Published,
            Difficulty = Difficulty.Beginner,
            EstimatedMinutes = 90,
            Version = 2,
            LearningObjectives = ["Identify what a contract requires of each party", "Recognise a variation and how it must be instructed", "Spot problematic payment and time-bar clauses"],
            Curriculum = new Curriculum { Title = "Australian Contract Administration", Description = "Practical contract administration for the Australian construction industry." },
            Modules =
            [
                new Module { Title = "Contract interpretation", Order = 0, Lessons = [new Lesson { Title = "Reading a construction contract", Order = 0, Competencies = [interpretation] }] },
                new Module
                {
                    Title = "Variations and payment", Order = 1,
                    Lessons =
                    [
                        new Lesson { Title = "Variations", Order = 0, Competencies = [variations] },
                        new Lesson { Title = "Progress claims and payment schedules", Order = 1, Competencies = [payment] },
                    ],
                },
            ],
        };

        foreach (var c in new[] { interpretation, variations, payment }) c.Course = course; // competency.CourseId is required

        var quiz = new Quiz { Course = course, Title = "Fundamentals quiz", Kind = QuizKind.Quiz, Description = "Four short questions. Retake as often as you like.", PassPercent = 70 };
        var check = new Quiz { Course = course, Title = "Quick check: variations", Kind = QuizKind.KnowledgeCheck, PassPercent = 50 };
        var exam = new Quiz { Course = course, Title = "Final examination", Kind = QuizKind.Exam, PassPercent = 75, ImmediateFeedback = false, TimeLimitMinutes = 20, MaxAttempts = 3, QuestionCount = 3 };
        var exercise = new ExerciseConfiguration
        {
            Course = course,
            TypeKey = "contract-review",
            Title = "Review a subcontract",
            Competencies = [payment, interpretation],
            ConfigJson = Json(new
            {
                scenario = "You are the contract administrator for a head contractor. A subcontractor has sent you this draft subcontract for review.",
                instructions = "Flag the clauses you think are problematic. Be careful: flagging a sound clause costs a point.",
                clauses = new[]
                {
                    new { id = "c1", title = "1. Payment", text = "The Contractor will pay each progress claim within 10 business days after the Contractor has itself been paid by the Principal for the same work." },
                    new { id = "c2", title = "2. Variations", text = "The Subcontractor must not carry out any variation unless directed in writing by the Contractor's Representative." },
                    new { id = "c3", title = "3. Extensions of time", text = "Any claim for an extension of time is barred unless it is made within 24 hours of the delay event." },
                    new { id = "c4", title = "4. Disputes", text = "A dispute will first be referred to senior representatives of each party before any proceedings are commenced." },
                },
                issues = new[]
                {
                    new { clauseId = "c1", explanation = "Making payment depend on the head contractor being paid (pay-when-paid) is generally restricted by security of payment legislation in Australian jurisdictions.", points = 2 },
                    new { clauseId = "c3", explanation = "A 24-hour time bar is very short and could cost the subcontractor legitimate time. Time bars are a common red flag in contract review.", points = 1 },
                },
            }),
        };

        db.Courses.Add(course);
        db.Banks.Add(bank);
        bank.Course = course;
        db.Exercises.Add(exercise);
        quiz.Questions = [.. bank.Questions.Select(q => new QuizQuestion { Question = q })];
        check.Questions = [.. bank.Questions.Take(2).Select(q => new QuizQuestion { Question = q })];
        exam.Questions = [.. bank.Questions.Select(q => new QuizQuestion { Question = q })];
        db.Quizzes.AddRange(quiz, check, exam);
        await db.SaveChangesAsync(); // ids exist now, so blocks can reference the quizzes and exercise

        ContentBlock Block(Lesson l, string type, int order, object data) => new() { Lesson = l, Type = type, Order = order, DataJson = Json(data) };
        var lessons = course.Modules.SelectMany(m => m.Lessons).ToList();
        db.Blocks.AddRange(
            Block(lessons[0], "richtext", 0, new { markdown = "# Reading a construction contract\n\nA contract tells you **who must do what, by when, and what happens if they do not**. Start with the **definitions**, the **scope of works** and the **payment and time** provisions, then read the notice requirements closely.\n\n- Note every deadline and who it binds\n- Note every notice and the form it must take\n- Note the order of precedence between documents" }),
            Block(lessons[0], "callout", 1, new { variant = "tip", text = "Keep a one-page summary of key dates and notice requirements for every contract you administer." }),
            Block(lessons[1], "richtext", 0, new { markdown = "# Variations\n\nA **variation** is a change to the scope of the works. Most contracts require it to be **instructed in a particular way**, often in writing, before the work is done." }),
            Block(lessons[1], "callout", 1, new { variant = "warning", text = "Work done without following the contract's variation procedure may not be recoverable." }),
            Block(lessons[1], "knowledgecheck", 2, new { quizId = check.Id }),
            Block(lessons[2], "richtext", 0, new { markdown = "# Progress claims and payment schedules\n\nProgress claims set out what the claimant says it is owed. The respondent answers with a **payment schedule**. Deadlines are short and the rules differ between jurisdictions, so always check the legislation and the contract." }),
            Block(lessons[2], "exercise", 1, new { exerciseId = exercise.Id }),
            Block(lessons[2], "quiz", 2, new { quizId = quiz.Id }),
            Block(lessons[2], "quiz", 3, new { quizId = exam.Id }));
        await db.SaveChangesAsync();
    }
}
