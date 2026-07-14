Feature: Location search and discovery
  As a signed-in user
  I want to search for potential meetup destinations
  So that my team can explore where to meet

  Background:
    Given a signed-in user "amara@cyclesync.example"

  @phase2 @locations
  Scenario: Search returns matching destinations from Azure Maps
    Given Azure Maps returns results for "Lisbon"
    When the user searches for "Lisbon"
    Then the results include a location named "Lisbon, Portugal"
    And each result has coordinates and a country

  @phase2 @locations
  Scenario: Selecting a search result persists the location
    Given Azure Maps returns results for "Lisbon"
    When the user searches for "Lisbon"
    And the user selects "Lisbon, Portugal"
    Then a persistent location "Lisbon, Portugal" exists
    And it is visible to all users

  @phase2 @locations
  Scenario: Selecting an already-known location does not duplicate it
    Given a persistent location "Lisbon, Portugal" already exists
    When the user searches for "Lisbon"
    And the user selects "Lisbon, Portugal"
    Then there is exactly one location "Lisbon, Portugal"

  @phase2 @locations @principle-permanence
  Scenario: Locations are never deleted
    Given a persistent location "Lisbon, Portugal" exists
    When any user attempts to delete "Lisbon, Portugal"
    Then the operation is not permitted
    And the location "Lisbon, Portugal" still exists
