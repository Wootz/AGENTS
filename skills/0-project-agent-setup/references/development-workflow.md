# Project Workflow Choices

Read this when recommending project workflow skills or updating their routing. Each skill is independently usable. The user chooses which skills and stages the project adopts; a recommendation or an installed skill does not create a dependency on another skill or require an execution sequence.

## Preserve existing project decisions

Read any existing `Agent skills` block and the documents it points to. Preserve actual tracker, optional label, domain-document, and development-rule choices regardless of which tool or person produced them. Existing output from another skill is useful context, not a prerequisite for running this skill.

When necessary project information is missing, obtain the information itself. Do not require the user to run a setup skill to supply it. Reusing configuration does not authorize tracker changes, label creation, or Git remote changes.

## Project specs

For project specification work, use the current `0-generate-spec` source and export it as `generate-spec`. Read its actual instructions and resources at execution time; do not embed a snapshot here or choose between spec producers.

`to-spec` is reserved for the owner's personal local use. Exclude it from project skill proposals, copies, and indexes; leave the local source untouched. If an existing project copy needs removal during an update, identify it in the proposed changes and preserve unrelated content.

## Recommend planning where useful

Recommend writing a Plan before implementing feature work. The user decides whether to adopt `0-generate-plan` (exported as `generate-plan`), another planning method, or a different workflow. Copy and document only the skills and gates the project adopts.

If the project chooses a Spec-and-Plan workflow, a useful sequence is:

`Requirements → Spec approval → Plan approval → Implementation → Verification`

Read a selected planning skill's current instructions to verify its actual input and output requirements. If the proposed combination has incompatible document locations or formats, report the gap and obtain a project-specific decision; leave global source skills unchanged. Pure engineering work may be planned directly when supported by the selected skill; routine edits retain their task-specific scope. Record only the project's adopted gates and preserve the selected skill's safeguards. Publishing issues or changing tracker state still requires authorization for that action.
