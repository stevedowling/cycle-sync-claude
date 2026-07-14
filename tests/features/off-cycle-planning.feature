Feature: Off-cycle planning
  As a signed-in user
  I want to create and manage meetup events
  So that the team can commit to a place and dates

  Background:
    Given a signed-in user "amara@cyclesync.example"
    And a persistent location "Lisbon, Portugal" exists

  @phase4 @offcycle
  Scenario: Create an off-cycle for a location and date range
    When the user creates an off-cycle "Autumn Meetup" at "Lisbon, Portugal" from "2026-10-05" to "2026-10-09"
    Then an off-cycle "Autumn Meetup" exists at "Lisbon, Portugal"
    And its dates are "2026-10-05" to "2026-10-09"
    And it is visible to all users

  @phase4 @offcycle
  Scenario: The creator is enrolled with a default attendance status
    When the user creates an off-cycle "Autumn Meetup" at "Lisbon, Portugal" from "2026-10-05" to "2026-10-09"
    Then "amara@cyclesync.example" has attendance status "Interested" for "Autumn Meetup"

  @phase4 @offcycle
  Scenario: Reject an off-cycle whose end date precedes its start date
    When the user creates an off-cycle "Broken" at "Lisbon, Portugal" from "2026-10-09" to "2026-10-05"
    Then the operation is rejected with reason "end date must not precede start date"

  @phase4 @offcycle
  Scenario: Edit the dates of an off-cycle
    Given an off-cycle "Autumn Meetup" at "Lisbon, Portugal" from "2026-10-05" to "2026-10-09"
    When the user changes the dates to "2026-10-12" to "2026-10-16"
    Then its dates are "2026-10-12" to "2026-10-16"
    And cost estimates are recalculated for the new dates
