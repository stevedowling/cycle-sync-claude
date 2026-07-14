Feature: User profile management
  As a signed-in user
  I want to manage my profile
  So that travel intelligence and costs are tailored to me

  Background:
    Given a signed-in user "amara@cyclesync.example"

  @phase1 @profile
  Scenario: A new user gets a profile seeded from Google
    When the user signs in for the first time
    Then a profile exists with their name and email
    And the profile has no passports yet

  @phase1 @profile
  Scenario: Set home location, currency and language
    When the user sets their home location to "Auckland, New Zealand"
    And sets their preferred currency to "NZD"
    And sets their preferred language to "en-NZ"
    Then the profile reflects home location "Auckland, New Zealand"
    And the profile reflects currency "NZD"
    And the profile reflects language "en-NZ"

  @phase1 @profile
  Scenario: Add multiple passports
    When the user adds a passport for "New Zealand"
    And adds a passport for "United Kingdom"
    Then the profile lists 2 passports
    And the passports include "New Zealand" and "United Kingdom"

  @phase1 @profile
  Scenario: Remove a passport
    Given the user holds passports for "New Zealand" and "United Kingdom"
    When the user removes the "United Kingdom" passport
    Then the profile lists 1 passport
    And the passports include "New Zealand"

  @phase1 @profile @privacy
  Scenario: Profiles are visible to all authenticated users
    Given another signed-in user "bao@cyclesync.example"
    When "bao@cyclesync.example" views the profile of "amara@cyclesync.example"
    Then they can see the home location and passports
