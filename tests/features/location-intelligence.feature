Feature: Location intelligence
  As a signed-in user
  I want climate, travel and visa insight for a location
  So that I can judge whether it is a good meetup destination

  Background:
    Given a signed-in user "amara@cyclesync.example"
    And the user holds a passport for "New Zealand"
    And a persistent location "Lisbon, Portugal" exists

  @phase2 @intelligence
  Scenario: AI-generated intelligence is produced and timestamped
    When the user opens the "Lisbon, Portugal" details
    Then AI-generated intelligence is shown
    And it includes climate summary and best times to visit
    And it shows the generation timestamp
    And it shows a confidence indicator

  @phase2 @intelligence
  Scenario: Cached intelligence is reused rather than regenerated
    Given intelligence for "Lisbon, Portugal" was generated 1 day ago
    When the user opens the "Lisbon, Portugal" details
    Then the previously generated intelligence is shown
    And no new generation is triggered

  @phase2 @intelligence @visa
  Scenario: Visa requirements are shown for the user's passport
    When the user opens the "Lisbon, Portugal" details
    Then visa guidance is shown for a "New Zealand" passport holder

  @phase2 @intelligence @principle-transparency
  Scenario: Estimates always disclose confidence and freshness
    When the user opens the "Lisbon, Portugal" details
    Then every AI-generated figure displays a confidence level
    And every AI-generated figure displays when it was generated
