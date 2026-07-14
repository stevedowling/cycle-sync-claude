Feature: Authentication
  As a team member
  I want to sign in with my company Google account
  So that only my colleagues can access CycleSync

  Background:
    Given the workspace is restricted to the "cyclesync.example" domain

  @phase1 @auth
  Scenario: A user from the allowed domain signs in
    Given a Google account "amara@cyclesync.example"
    When the user completes Google sign-in
    Then a CycleSync session is established
    And the user has full access rights

  @phase1 @auth
  Scenario: A user from a disallowed domain is rejected
    Given a Google account "outsider@gmail.com"
    When the user completes Google sign-in
    Then access is denied with reason "domain not permitted"
    And no CycleSync session is established

  @phase1 @auth
  Scenario: An unauthenticated request cannot reach protected data
    Given no active session
    When I request the list of locations
    Then the response status is 401

  @phase1 @auth @principle-equal-access
  Scenario: All authenticated users have identical rights
    Given a signed-in user "amara@cyclesync.example"
    And a signed-in user "bao@cyclesync.example"
    Then neither user has administrative privileges over the other
    And both users can perform the same actions
