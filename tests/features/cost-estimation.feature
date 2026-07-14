Feature: Cost estimation
  As a signed-in user
  I want cost estimates for travelling to a location
  So that I can understand the financial impact of a meetup

  Background:
    Given a signed-in user "amara@cyclesync.example"
    And the user's home location is "Auckland, New Zealand"
    And the user's preferred currency is "NZD"
    And a persistent location "Lisbon, Portugal" exists

  @phase4 @cost
  Scenario: A heuristic estimate is produced for a location
    When the user views the cost estimate for "Lisbon, Portugal"
    Then an estimate is shown for flights, accommodation and daily expenses
    And the estimate is expressed in "NZD"

  @phase4 @cost @principle-transparency
  Scenario: Estimates disclose confidence and generation time
    When the user views the cost estimate for "Lisbon, Portugal"
    Then the estimate shows a confidence level
    And the estimate shows when it was generated

  @phase4 @cost
  Scenario: Estimates are recalculated for specific off-cycle dates
    Given an off-cycle "Autumn Meetup" at "Lisbon, Portugal" from "2026-10-05" to "2026-10-09"
    When the user views the cost estimate for "Autumn Meetup"
    Then accommodation and daily expenses are calculated for 4 nights
    And the estimate is expressed in the user's currency "NZD"

  @phase4 @cost
  Scenario: Estimates reflect the traveller's home location
    Given another signed-in user "bao@cyclesync.example" whose home location is "London, United Kingdom"
    And an off-cycle "Autumn Meetup" at "Lisbon, Portugal" from "2026-10-05" to "2026-10-09"
    Then the flight estimate for "amara@cyclesync.example" differs from that for "bao@cyclesync.example"
