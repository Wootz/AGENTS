# Project Agent Instructions

All AI responses, human-facing documents, and code comments must use Traditional Chinese (正體中文). Agent-facing files, including skills and this file, use English.

## Project Context

{{project_context}}

## Required References

{{required_references_with_reading_conditions}}

## Operating Rules

{{applicable_project_and_global_rules}}

## Development Workflow

{{confirmed_workflow_and_completion_gates}}

## Project Skills

Project skills are physical copies in `.agents/skills/`, with the source names' leading `0-` removed. Each skill is independently usable; follow the workflow the user has chosen for this project. When using a skill, Codex and Claude Code must read its project-local `SKILL.md`, then follow its conditional references. Resolve paths from the workspace root; resource paths inside a skill are relative to that skill. Use the project name listed below and prefer the project's adapted copy over its global source. Load only the skills needed for the current task.

{{installed_skill_table_with_name_trigger_and_path}}
