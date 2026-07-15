Feature: Error taxonomy
  As a client of the CycleSync API
  I want every failure to be a typed RFC 7807 problem detail
  So that the SPA can branch on the kind of error rather than parsing prose

  Background:
    Given a signed-in user "amara@cyclesync.example"

  @phase5 @hardening @error-taxonomy
  Scenario: A missing off-cycle is a typed not-found problem
    When the user requests an off-cycle that does not exist
    Then the response is a problem of type "not-found" with status 404
    And the problem carries a human-readable title and detail

  @phase5 @hardening @error-taxonomy
  Scenario: A missing location is a typed not-found problem
    When the user requests a location that does not exist
    Then the response is a problem of type "not-found" with status 404
    And the problem carries a human-readable title and detail

  @phase5 @hardening @error-taxonomy
  Scenario: An invalid off-cycle is a typed validation problem
    Given a persistent location "Lisbon, Portugal" exists
    When the user creates an off-cycle "Backwards Meetup" at "Lisbon, Portugal" from "2026-10-09" to "2026-10-05"
    Then the response is a problem of type "validation" with status 400
    And the problem carries a human-readable title and detail

  @phase5 @hardening @error-taxonomy
  Scenario: Creating an off-cycle for an unknown location is a typed not-found problem
    When the user creates an off-cycle "Ghost Meetup" for a location that does not exist
    Then the response is a problem of type "not-found" with status 404
    And the problem carries a human-readable title and detail
