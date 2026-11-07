# Build Plan

Build Specification — Modular Professional Training Platform
MVP direction defined
Version 0.1

1. Project overview
   1.1 Purpose
   Build a generic, modular online training platform designed to deliver professional education and competency development through interchangeable curricula.

The initial curriculum will focus on Contract Administration in the Australian construction industry, with the underlying platform designed to support additional professional training programmes in the future.

The platform will provide two distinct interfaces:

1. Learner Application — for discovering courses, consuming educational content, completing assessments and practical exercises, and tracking competency development.
2. Course Builder — for teachers and authorised administrators to create, manage, assess and publish courses using a block-based content authoring system.
   Both interfaces will operate on shared application infrastructure, content storage, assessment services and user management.
   1.2 Core philosophy
   The platform should prioritise practical knowledge and demonstrated competency over passive course completion.

Its learning model is based on the following principles:

- Learners are free to choose where they begin.
- No mandatory initial diagnostic examination.
- No course or lesson prerequisites.
- Knowledge is progressively assessed through short quizzes and practical exercises.
- Assessment results contribute to an evolving competency profile.
- Practical simulations provide opportunities to demonstrate workplace capabilities.
- Teachers can monitor learner performance and identify knowledge gaps.
  The platform should distinguish between completing educational content and actually demonstrating competency.
  1.3 Initial scope
  The first implementation will establish the generic platform infrastructure.

The initial Contract Administration curriculum will serve as the primary example and validation environment, but curriculum-specific functionality must not be hardcoded into the platform.

2. Application architecture
   2.1 High-level architecture
   The platform consists of the following logical subsystems:
   Professional Training Platform
   Shared backend and application infrastructure
   Learner Interface
   Catalogue, courses, lessons, exercises, assessments and progress.

Course Builder
Authoring, curriculum management, question banks and publishing.

Content Engine
Reusable educational blocks and media management.

Assessment Engine
Quizzes, examinations, scoring and competency evaluation.

Exercise / Simulation Engine
Extensible registry of reusable interactive exercise components.

Shared Infrastructure
Authentication · Permissions · Database · Storage · Analytics · Course catalogue
Interchangeable Curricula
Australian Contract Administration and future professional training programmes

2.2 Architectural requirements

- Learner and Course Builder interfaces must be separate application experiences.
- Both interfaces must use the same backend services.
- Curriculum content must be data-driven.
- Educational content must not be hardcoded into application components.
- Assessment functionality must be reusable across courses.
- Specialised exercises must be independently implementable components.
- New course types and curricula must not require modifications to the core learner experience.
- Permissions must be enforced server-side, not just through interface visibility.

3. User accounts and permissions
   3.1 Account types
   The platform must support three primary account roles.
   Role Description
   Learner Consumes courses, completes lessons, participates in exercises and tracks progress.
   Teacher / Author Creates and manages courses, educational content, questions and practical exercises.
   Administrator Manages the platform, users, permissions, course publication and configuration.

A user may eventually hold multiple roles.
3.2 Learner permissions
Learners must be able to:

- Register and manage their account.
- Browse the shared course catalogue.
- Enrol in available courses.
- Access public courses and authorised private courses.
- Start courses at any module or lesson.
- Complete lessons and assessments in any order.
- Attempt quizzes repeatedly.
- Participate in interactive exercises.
- View their course progress.
- View their competency profile.
- Review previous assessment results and feedback.
  3.3 Teacher permissions
  Teachers must be able to:
- Create new courses.
- Edit courses they own or have permission to manage.
- Create and organise modules and lessons.
- Add and configure educational content blocks.
- Create and manage question banks.
- Create examinations.
- Add and configure practical exercises.
- Preview courses as a learner.
- Save courses as drafts.
- Publish courses, subject to applicable publication permissions.
- View learner enrolments and performance.
- Review individual learner assessment results.
- Identify competency gaps.
- Manually review and assess practical exercises where required.
  Teachers must not automatically have access to private courses owned by unrelated organisations.
  3.4 Administrator permissions
  Administrators must be able to:
- Manage user accounts and roles.
- Manage teacher permissions.
- View and manage all courses.
- Approve or reject courses for publication where approval is enabled.
- Archive and unpublish courses.
- Manage course visibility.
- Manage organisations and their memberships.
- Configure platform-wide settings.
- Access platform-wide reporting.
  3.5 Organisations and ownership
  The platform should support organisational ownership.

A course may be:

- Public: Available through the shared course catalogue.
- Private: Restricted to authorised users or members of an organisation.
  Course ownership must support individual teachers and organisations.

Multiple teachers should be able to collaborate on a course through assigned permissions.

4. Curriculum and course structure
   4.1 Hierarchical content structure
   The curriculum must support a flexible hierarchical structure.

Proposed hierarchy:
Curriculum
Australian Contract Administration

Course
Contract Administration Fundamentals

Modules
Contract Interpretation · Procurement · Variations

Lessons
Individual educational units

Content Blocks
Text · Images · Video · Quizzes · Exercises · Simulations

A curriculum may contain multiple courses.

A course contains modules, modules contain lessons, and lessons contain ordered content blocks.
4.2 Course metadata
Every course should support:

- Course title.
- Description.
- Cover image.
- Curriculum/category.
- Author and contributing teachers.
- Course visibility.
- Publication status.
- Difficulty level.
- Estimated duration.
- Learning objectives.
- Associated competencies.
- Version information.
- Creation and modification timestamps.
  4.3 Course navigation
  The platform must not enforce prerequisites or mandatory learning sequences.

Learners must be able to:

- Access any module.
- Access any lesson.
- Complete lessons in any order.
- Repeat completed lessons.
- Attempt assessments independently of lesson completion, where the course configuration allows it.
  The system should still present recommended learning sequences without restricting access.

5. Block-based Course Builder
   5.1 General requirements
   The Course Builder must provide a visual, block-based content authoring interface.

Teachers should be able to construct lessons by adding, configuring, editing, reordering and removing reusable blocks.

The builder should separate educational content from the application components used to display that content.

Each block must have:

- A defined block type.
- A configurable data structure.
- A validation schema.
- A learner-facing renderer.
- Authoring controls.
- Optional assessment or competency associations.
  The block system must be extensible so that new block types can be introduced without redesigning the entire Course Builder.
  5.2 Initial supported block types
  Block Purpose
  Rich text Educational text and explanations
  Image Diagrams, photographs and illustrations
  Video Embedded or hosted instructional videos
  Document Supporting PDFs and reference material
  Callout Important information, warnings and tips
  Knowledge check Short questions embedded within lessons
  Quiz Structured knowledge assessment
  Exercise Interactive educational activity
  Simulation Specialised practical training experience

5.3 Block configuration
Each block must support its own configuration interface.

Examples:

- Text blocks support formatting, headings and lists.
- Image blocks support image uploads, captions and alternative text.
- Video blocks support video sources, captions and descriptions.
- Quiz blocks allow teachers to select questions from question banks.
- Exercise blocks allow teachers to configure the exercise and its parameters.
- Simulation blocks allow teachers to select a simulation type and configure its scenario.
  5.4 Builder usability
  The Course Builder should support:
- Drag-and-drop block ordering.
- Live content preview.
- Draft saving.
- Content validation.
- Module and lesson reordering.
- Copying and duplicating blocks.
- Reusing existing blocks where appropriate.
- Previewing the learner experience.
- Publishing and unpublishing courses.
  5.5 Content versioning
  Published courses should support versioning.

Editing an existing published course must not silently alter the historical assessment context or learner records.

The system should retain sufficient version information to identify which course and assessment versions a learner completed.

6. Assessment Engine
   6.1 General requirements
   The Assessment Engine must be independent of individual curricula.

It should provide reusable assessment functionality that can be configured and used across any course.

The engine must support:

- Standalone quizzes.
- Embedded knowledge checks.
- Module assessments.
- Final course examinations.
- Question banks.
- Randomised question selection.
- Unlimited attempts.
- Automated scoring.
- Answer explanations.
- Assessment history.
  6.2 Initial question types
  The initial implementation should support:
  Question type Description
  Single choice Select one correct answer
  Multiple choice Select multiple correct answers
  True / False Evaluate a statement
  Fill in the blank Enter a missing word or value
  Matching Match related terms and definitions
  Ordering Arrange steps or events in sequence
  Numerical Enter a calculated value
  Scenario-based Answer questions based on a supplied scenario

The question system must allow additional question types to be added later.
6.3 Question configuration
Questions should support:

- Question text.
- Optional supporting images.
- Optional supporting documents.
- Answer options or expected responses.
- Correct answer definitions.
- Explanations for correct and incorrect answers.
- Optional hints.
- Difficulty classification.
- Associated competencies.
- Question categories and tags.
- Marks or scoring weights.
  6.4 Quiz behaviour
  Quizzes must support:
- Unlimited attempts.
- Randomised question selection.
- Randomised answer ordering where appropriate.
- Immediate feedback.
- Explanations after answers are submitted.
- Attempt history.
- Score tracking.
- Configurable passing thresholds where applicable.
  Question repetition rules:

The system must avoid presenting the same question twice consecutively to the same learner within the same assessment context.

Where possible, question selection should also avoid recently presented questions.

The question selection algorithm must account for the available question pool. If only one eligible question exists, the system may reuse it rather than preventing the assessment from continuing.
6.5 Question banks
Teachers must be able to maintain reusable question banks.

Question banks should support:

- Course-specific questions.
- Module-specific questions.
- Competency-specific questions.
- Question categories.
- Difficulty levels.
- Random selection rules.
- Reuse across lessons and examinations.
  6.6 Final examinations
  Courses may include a final examination.

Teachers must be able to configure:

- Examination questions or question pools.
- Number of questions.
- Marks per question.
- Passing percentage.
- Time limit, if required.
- Attempt limits, if required.
- Randomisation rules.
- Feedback settings.
- Examination availability.
  Final examinations should remain optional course components rather than mandatory features of every course.

7. Competency Tracking and Knowledge Gaps
   7.1 General requirements
   The platform must track demonstrated competency independently of course completion.

Competencies represent individual professional skills, knowledge areas or capabilities.

Each course may define its own competency framework.

For example, an Australian Contract Administration curriculum might include:

- Contract interpretation.
- Procurement and tendering.
- Subcontract administration.
- Variation assessment.
- Extensions of time.
- Progress claims.
- Payment schedules.
- Retention management.
- Contractual risk.
- Dispute resolution.
  The underlying competency engine must remain curriculum-independent.
  7.2 Competency associations
  Teachers must be able to associate content with one or more competencies.

This includes:

- Lessons.
- Knowledge checks.
- Quizzes.
- Examinations.
- Practical exercises.
- Simulations.
  Assessment results must contribute to the relevant competency records.
  7.3 Competency evaluation
  Competency should be evaluated using a combination of:

1. Knowledge assessment results.
2. Repeated assessment performance.
3. Practical exercise results.
4. Simulation outcomes.
5. Teacher-assessed results where applicable.
   The platform must support different assessment weights and competency evaluation rules.

The exact competency scoring algorithm should be configurable and should not be hardcoded to one curriculum.
7.4 Competency states
The platform should initially support the following competency states:
State Meaning
Not assessed No relevant assessment evidence exists
Developing Some understanding demonstrated, but evidence is insufficient
Demonstrated Sufficient evidence of competency
Requires review Evidence requires teacher evaluation

Competency states should be derived from assessment evidence and configurable evaluation rules.

A learner's competency profile must evolve as they complete additional training.
7.5 Learner competency dashboard
Learners should be able to view:

- Their competencies.
- Current competency status.
- Assessment history.
- Areas requiring further development.
- Relevant recommended lessons.
- Relevant exercises and simulations.
  The platform should recommend learning activities based on identified knowledge gaps, without restricting access to other content.
  7.6 Teacher competency dashboard
  Teachers should be able to view:
- Individual learner competency profiles.
- Assessment performance.
- Knowledge gaps.
- Lesson completion.
- Exercise results.
- Competency progression.
- Learners requiring additional attention.
  Teachers should also be able to view aggregated performance across a course.

8. Exercise and Simulation Engine
   8.1 General requirements
   The Exercise Engine must support specialised, interactive training experiences without coupling them to the core curriculum infrastructure.

Exercises should be treated as reusable application components.

The platform should provide a standard integration interface for these components.

A teacher should be able to select an exercise type from the Course Builder, configure its parameters and insert it into a lesson.
8.2 Exercise architecture
The system should distinguish between:

Exercise component: The reusable interactive functionality developed by the platform.

Exercise configuration: The scenario, instructions, supporting materials, parameters and assessment criteria supplied by the teacher.

Exercise attempt: The individual learner's interaction with the exercise and its resulting evidence.
8.3 Exercise capabilities
The exercise framework should support:

- Instructions and objectives.
- Scenario descriptions.
- Supporting documents and images.
- Interactive learner inputs.
- Multiple exercise stages.
- Automated evaluation.
- Manual teacher evaluation.
- Feedback and explanations.
- Attempt history.
- Competency associations.
- Structured results.
  8.4 Initial exercise types
  The framework must initially accommodate:
- Document review exercises.
- Numerical calculation exercises.
- Scenario-based decision exercises.
- Document completion exercises.
- Multi-stage practical simulations.
  These are initial supported exercise categories, not a requirement to build every specialised exercise for the first release.
  8.5 Specialised simulation example
  An Australian Contract Administration course might include a variation assessment simulation.

A teacher configures:

- Project scenario.
- Original contract documents.
- Subcontractor variation submission.
- Supporting correspondence.
- Relevant contractual provisions.
- Assessment criteria.
- Expected outcomes.
  The learner interacts with the simulation and completes the required tasks.

The simulation produces structured assessment results which can contribute to competency tracking.
8.6 Extensibility
The Exercise Engine must allow new exercise components to be introduced independently.

Adding a specialised simulation must not require rewriting:

- The Course Builder.
- The course content model.
- The assessment engine.
- The learner dashboard.
- The competency system.
  The platform should use a documented exercise integration contract.

9. Course Catalogue and Publishing
   9.1 Shared course catalogue
   The platform must provide a shared course catalogue where learners can discover available training programmes.

The catalogue should support:

- Browsing published courses.
- Searching by course title and description.
- Filtering by curriculum and category.
- Filtering by difficulty.
- Viewing course descriptions.
- Viewing learning objectives.
- Viewing estimated duration.
- Viewing author information.
- Enrolling in courses.
- Accessing previously enrolled courses.
  9.2 Course visibility
  Courses must support two visibility settings:

Public

- Appears in the shared course catalogue.
- Available to registered learners.
- Can be discovered by all platform users.
  Private
- Does not appear in the public catalogue.
- Accessible only to authorised users.
- May be restricted to specific organisations or invited learners.
  9.3 Publication workflow
  Courses must support three publication states:
  Status Behaviour
  Draft Only accessible to authorised authors and administrators.
  Published Available to learners according to visibility settings.
  Archived Removed from new enrolment and catalogue discovery, while historical records are retained.

Administrators should be able to configure whether teacher-created courses require approval before publication.
9.4 Course enrolment
Learners should be able to enrol in multiple courses simultaneously.

The platform must support:

- Course enrolment records.
- Enrolment timestamps.
- Course access permissions.
- Course progress tracking.
- Historical enrolment records.
- Unenrolment where permitted.
  There must be no requirement to complete one course before enrolling in another.

10. Learner Application
    10.1 General requirements
    The Learner Application is the primary interface for consuming educational content.

It should be designed around ease of navigation, readability and practical learning.

The interface should be responsive and support desktop, tablet and mobile devices.
10.2 Learner dashboard
The dashboard should provide:

- Enrolled courses.
- Recently accessed courses.
- Continue learning functionality.
- Course completion progress.
- Competency overview.
- Identified knowledge gaps.
- Recommended learning activities.
- Recent assessment results.
  10.3 Course learning interface
  The learning interface should provide:
- Course overview.
- Module and lesson navigation.
- Lesson content rendering.
- Content block rendering.
- Embedded quizzes.
- Exercise and simulation integration.
- Lesson completion tracking.
- Previous and next lesson navigation.
- Course-wide progress indicators.
  Learners must be able to navigate directly to any lesson.
  10.4 Lesson completion
  The platform should track whether a learner has completed a lesson.

Lesson completion should be separate from competency achievement.

Completion may be based on configurable rules, such as viewing required content or completing an associated activity.

The platform should not automatically assume that viewing content demonstrates knowledge.
10.5 Learning history
Learners should have access to their historical learning activity, including:

- Completed lessons.
- Quiz attempts.
- Examination results.
- Exercise attempts.
- Simulation results.
- Competency progression.

11. Course Builder Application
    11.1 General requirements
    The Course Builder must be a separate interface from the Learner Application.

It should provide an administrative workspace for designing, managing and publishing educational programmes.
11.2 Course management
Teachers should be able to:

- Create courses.
- Edit course metadata.
- Manage course visibility.
- Organise modules.
- Create and organise lessons.
- Manage learning objectives.
- Associate competencies.
- Preview courses.
- Publish courses.
- Archive courses where authorised.
  11.3 Content authoring
  The content editor should support:
- Creating content blocks.
- Reordering blocks.
- Editing block configurations.
- Uploading images and documents.
- Adding video sources.
- Embedding quizzes.
- Adding interactive exercises.
- Configuring simulations.
  11.4 Assessment authoring
  Teachers should have a dedicated assessment management interface.

This should support:

- Question creation.
- Question editing.
- Question categorisation.
- Question bank management.
- Answer configuration.
- Feedback authoring.
- Competency associations.
- Quiz configuration.
- Examination configuration.
  11.5 Exercise authoring
  Teachers should have an interface for configuring available exercise components.

The interface should dynamically present configuration fields based on the selected exercise type.

For example, a document review simulation might require document uploads, review instructions and evaluation criteria.

A numerical exercise might require input parameters, expected calculations and acceptable answers.

The Course Builder should not need to understand the internal implementation of every specialised exercise.

12. Teacher Analytics and Reporting
    12.1 Individual learner reporting
    Teachers must be able to inspect individual learner records within courses they are authorised to manage.

Information should include:

- Enrolment information.
- Course progress.
- Lesson completion.
- Assessment history.
- Quiz scores.
- Examination results.
- Exercise submissions.
- Competency profile.
- Identified knowledge gaps.
  12.2 Course-level reporting
  Teachers should be able to view aggregated course performance.

Examples include:

- Number of enrolled learners.
- Course completion rates.
- Average assessment scores.
- Most frequently failed questions.
- Competencies with the lowest demonstrated performance.
- Exercise completion rates.
- Learners requiring additional attention.
  12.3 Assessment insights
  The platform should identify questions that may be problematic.

For example, teachers should be able to identify questions where a large proportion of learners consistently select incorrect answers.

This can help improve educational content and assessment quality.

13. Data Architecture and Core Entities
    The underlying data model must support interchangeable curricula, modular content, multiple account types and extensible exercise components.

The following represents the proposed logical entities. It is not a requirement to implement these as separate database tables without considering the final database design.
Entity Responsibility
User Account information
Role User permission classification
Organisation Organisational ownership and membership
OrganisationMembership User-to-organisation relationships
Curriculum Top-level educational programme
Course Individual training programme
CourseCollaborator Course authoring permissions
CourseVersion Published course versions
Module Course section
Lesson Educational unit
ContentBlock Individual content component
Competency Skill or knowledge classification
QuestionBank Collection of assessment questions
Question Individual assessment question
Quiz Configured assessment
QuizQuestion Question-to-quiz association
Examination Final assessment configuration
Enrolment Learner course registration
LessonProgress Lesson completion and activity
AssessmentAttempt Individual assessment attempt
AssessmentResponse Individual question response
CompetencyEvidence Evidence supporting competency
LearnerCompetency Learner competency state
ExerciseType Registered exercise component
ExerciseConfiguration Teacher-configured exercise
ExerciseAttempt Learner exercise session
ExerciseResult Exercise evaluation results
MediaAsset Uploaded images, videos and documents

13.1 Important data relationships
The data model must support:

- Multiple curricula per platform.
- Multiple courses per curriculum.
- Multiple modules per course.
- Multiple lessons per module.
- Multiple blocks per lesson.
- Multiple competencies per course.
- Multiple competencies per assessment.
- Reusable questions across assessments.
- Multiple attempts per learner.
- Multiple exercise types.
- Multiple exercise configurations per exercise type.
- Multiple learners per course.
- Multiple teachers per course.
- Public and private course ownership.
  13.2 Assessment history
  Assessment attempts must be retained as historical records.

New attempts must not overwrite previous results.

The platform must be able to reconstruct the questions, answers, scoring and relevant course version associated with a historical assessment.
13.3 Competency evidence
Competency should be derived from stored evidence rather than a manually maintained percentage alone.

The system must retain the underlying evidence supporting competency decisions.

This is important because competency evaluation rules may evolve as the platform develops.

14. Non-functional Requirements
    14.1 Maintainability

- Modular application architecture.
- Separation of curriculum data from application functionality.
- Reusable components.
- Well-defined service boundaries.
- Strong typing and validated data structures.
- Documented extension interfaces for content and exercise components.
  14.2 Security
- Secure authentication.
- Server-side role-based authorisation.
- Organisation-level data isolation.
- Private course access restrictions.
- Secure media access where required.
- Protection against unauthorised course modifications.
- Audit records for important administrative actions.
  14.3 Performance
- Efficient course and lesson loading.
- Appropriate media optimisation.
- Lazy loading for large educational content.
- Efficient question selection.
- Efficient learner progress queries.
- Support for concurrent learners.
  14.4 Accessibility
  The learner and authoring interfaces should follow WCAG 2.2 AA accessibility principles where practical.

This includes:

- Keyboard navigation.
- Accessible forms.
- Appropriate colour contrast.
- Alternative text for images.
- Captions and transcripts for video.
- Accessible assessment interactions.
  14.5 Data integrity
- Assessment records must not be lost when courses are edited.
- Course versions must be identifiable.
- Competency evidence must be traceable.
- Published course content must be validated.
- Exercise submissions must be associated with their correct exercise versions.

15. Initial MVP Scope
    The first release should focus on establishing the reusable platform infrastructure rather than attempting to build an extensive library of specialised Contract Administration simulations.
    15.1 MVP priorities
    Required for initial release
    Feature Priority
    User registration and authentication P0
    Account roles and permissions P0
    Organisation support P1
    Learner interface P0
    Course catalogue P0
    Public and private courses P0
    Course Builder P0
    Curriculum and course hierarchy P0
    Block-based content editor P0
    Text, image and video blocks P0
    Document attachments P1
    Question bank P0
    Initial question types P0
    Quiz engine P0
    Randomised question selection P0
    Immediate explanations and feedback P0
    Unlimited quiz attempts P0
    Final examinations P1
    Competency tracking P0
    Learner progress dashboard P0
    Teacher analytics P1
    Exercise component framework P0
    First specialised simulation P1
    Course versioning P1

P0 represents essential initial functionality. P1 represents functionality that should follow once the core infrastructure is established.
15.2 Initial content block implementation
The first release should implement:

1. Rich text.
2. Image.
3. Video.
4. Document attachment.
5. Callout.
6. Embedded knowledge check.
7. Quiz.
8. Exercise integration.
   The block architecture should accommodate future specialised components.
   15.3 Initial assessment implementation
   The first release should support:

- Single choice.
- Multiple choice.
- True / False.
- Fill in the blank.
- Numerical answers.
  Matching, ordering and more complex scenario-based question types can follow.
  15.4 Initial exercise implementation
  The initial release must establish the reusable exercise integration architecture.

At least one functional exercise should validate the architecture.

A simple contract review exercise would be a suitable starting point.

16. Explicitly Out of Scope
    The following features should not be included in the initial implementation unless separately approved:

- AI-generated courses.
- AI-generated assessment questions.
- Adaptive AI tutoring.
- Dynamic onboarding-based learning paths.
- Automatic initial diagnostic examinations.
- Mandatory learning sequences.
- Course prerequisites.
- Accredited qualification management.
- External professional certification integrations.
- Automated employment or recruitment functionality.
- Complex gamification systems.
- Social networking features.
- Live classroom functionality.
- Subscription billing and payment processing.
- Native mobile applications.
  The platform should not be architecturally restricted from supporting these capabilities in the future.

17. Future Extensibility
    The architecture should leave room for the following developments.
    Area Potential functionality
    AI learning assistance AI tutor for explaining contractual concepts
    Adaptive learning Automatically recommend lessons based on knowledge gaps
    Personalised onboarding Tailor learning recommendations to experience and career objectives
    Advanced simulations Multi-stage contractual negotiations and project scenarios
    Organisational training Employer dashboards and team competency reporting
    Gamification Achievements, streaks and learning milestones
    Accreditation Certificates and externally recognised qualifications
    Additional curricula QS, construction estimating, project management and other industries

These are future possibilities, not requirements for the first release.

18. Initial Acceptance Criteria
    The MVP should be considered functionally complete when the following workflows can be demonstrated.
    Learner workflow

- [ ] A learner can register and log in.
- [ ] A learner can browse the course catalogue.
- [ ] A learner can enrol in a public course.
- [ ] A learner can access an authorised private course.
- [ ] A learner can navigate to any lesson without prerequisites.
- [ ] A learner can consume text, images, video and documents.
- [ ] A learner can complete an embedded quiz.
- [ ] A learner receives an explanation after answering.
- [ ] A learner can repeat quizzes indefinitely.
- [ ] The system avoids repeating the immediately preceding question where alternatives exist.
- [ ] A learner can complete a practical exercise.
- [ ] A learner can view course progress.
- [ ] A learner can view competency progress.
- [ ] A learner can review previous assessment results.
      Teacher workflow
- [ ] A teacher can create a course.
- [ ] A teacher can create modules and lessons.
- [ ] A teacher can construct lessons using reusable blocks.
- [ ] A teacher can reorder and configure content blocks.
- [ ] A teacher can create and manage question banks.
- [ ] A teacher can associate questions with competencies.
- [ ] A teacher can configure quizzes and examinations.
- [ ] A teacher can insert and configure exercises.
- [ ] A teacher can preview a course.
- [ ] A teacher can save a draft.
- [ ] A teacher can publish a course where authorised.
- [ ] A teacher can create private courses.
- [ ] A teacher can view individual learner performance.
- [ ] A teacher can identify competency gaps.
      Administrator workflow
- [ ] An administrator can manage users and roles.
- [ ] An administrator can manage organisations.
- [ ] An administrator can manage course visibility.
- [ ] An administrator can approve courses where required.
- [ ] An administrator can archive courses.
- [ ] An administrator can manage course ownership and permissions.
      Platform architecture
- [ ] Curriculum content is independent of application code.
- [ ] Learner and Course Builder interfaces are separate.
- [ ] Both interfaces use shared backend services.
- [ ] New block types can be added independently.
- [ ] New exercise components can be integrated independently.
- [ ] Assessment results are retained historically.
- [ ] Competency evidence is traceable.
- [ ] Published course versions can be identified.

19. Important architectural decisions still to be made
    This specification defines the product requirements and logical architecture. It intentionally doesn't prescribe a particular programming language, framework or database.

Before implementation, I'd want to make several technical decisions:

1. Application stack: Frontend framework, backend framework and database.
2. Exercise integration: How specialised exercise components register, receive configuration and return results.
3. Content schema: How block configurations are represented, validated and versioned.
4. Competency algorithm: How multiple assessments and practical exercises contribute to competency.
5. Course versioning: How published revisions interact with existing enrolments and assessment attempts.
6. Media storage: Upload handling, storage and video delivery.
   These can be addressed in a subsequent technical design document.

7. Product vision
   The long-term objective is to establish a reusable professional training platform capable of delivering high-quality, practical education across multiple industries.

The initial Australian Contract Administration curriculum will validate the platform.

The key architectural principle is:

Build the learning infrastructure once. Build educational content and specialised training experiences independently.

This should allow the platform to grow through additional curricula, exercises, simulations and assessment types without requiring fundamental changes to its underlying architecture.
