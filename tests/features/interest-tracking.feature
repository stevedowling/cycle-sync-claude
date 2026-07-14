Feature: Interest tracking
  As a signed-in user
  I want to mark locations I am interested in
  So that the team can find consensus destinations

  Background:
    Given a signed-in user "amara@cyclesync.example"
    And a persistent location "Lisbon, Portugal" exists
    And a persistent location "Tallinn, Estonia" exists

  @phase3 @interest
  Scenario: Mark a location as interested
    When the user marks interest in "Lisbon, Portugal"
    Then "Lisbon, Portugal" appears in the user's interested locations
    And the interest count for "Lisbon, Portugal" is 1

  @phase3 @interest
  Scenario: Marking interest is idempotent
    Given the user is already interested in "Lisbon, Portugal"
    When the user marks interest in "Lisbon, Portugal"
    Then the interest count for "Lisbon, Portugal" is 1

  @phase3 @interest
  Scenario: Remove interest
    Given the user is already interested in "Lisbon, Portugal"
    When the user removes interest in "Lisbon, Portugal"
    Then "Lisbon, Portugal" is not in the user's interested locations
    And the interest count for "Lisbon, Portugal" is 0

  @phase3 @interest
  Scenario: Interest counts aggregate across the team
    Given the user is already interested in "Lisbon, Portugal"
    And "bao@cyclesync.example" is interested in "Lisbon, Portugal"
    And "carlos@cyclesync.example" is interested in "Tallinn, Estonia"
    Then the interest count for "Lisbon, Portugal" is 2
    And the interest count for "Tallinn, Estonia" is 1

  @phase3 @interest
  Scenario: Locations can be sorted by consensus
    Given the user is already interested in "Lisbon, Portugal"
    And "bao@cyclesync.example" is interested in "Lisbon, Portugal"
    And "carlos@cyclesync.example" is interested in "Tallinn, Estonia"
    When the user sorts locations by interest
    Then "Lisbon, Portugal" is ranked above "Tallinn, Estonia"
