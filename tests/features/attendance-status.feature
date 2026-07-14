Feature: Attendance status tracking
  As a signed-in user
  I want to record whether I am coming to an off-cycle
  So that organisers can see who will attend

  Background:
    Given a signed-in user "amara@cyclesync.example"
    And an off-cycle "Autumn Meetup" at "Lisbon, Portugal" from "2026-10-05" to "2026-10-09"

  @phase4 @attendance
  Scenario Outline: Set my attendance status
    When the user sets their attendance to "<status>" for "Autumn Meetup"
    Then their attendance status for "Autumn Meetup" is "<status>"

    Examples:
      | status            |
      | Interested        |
      | Can't Make It     |
      | Probably Coming   |
      | Definitely Coming |
      | Booked            |

  @phase4 @attendance
  Scenario: Attendance can progress from interest to booked
    When the user sets their attendance to "Probably Coming" for "Autumn Meetup"
    And the user sets their attendance to "Definitely Coming" for "Autumn Meetup"
    And the user sets their attendance to "Booked" for "Autumn Meetup"
    Then their attendance status for "Autumn Meetup" is "Booked"

  @phase4 @attendance
  Scenario: A user can withdraw after committing
    Given the user has attendance status "Definitely Coming" for "Autumn Meetup"
    When the user sets their attendance to "Can't Make It" for "Autumn Meetup"
    Then their attendance status for "Autumn Meetup" is "Can't Make It"

  @phase4 @attendance
  Scenario: Reject an unknown status
    When the user sets their attendance to "Maybe Someday" for "Autumn Meetup"
    Then the operation is rejected with reason "unknown attendance status"

  @phase4 @attendance
  Scenario: Attendance is summarised per off-cycle
    Given "amara@cyclesync.example" has attendance status "Booked" for "Autumn Meetup"
    And "bao@cyclesync.example" has attendance status "Definitely Coming" for "Autumn Meetup"
    And "carlos@cyclesync.example" has attendance status "Can't Make It" for "Autumn Meetup"
    When anyone views the "Autumn Meetup" attendance summary
    Then it shows 1 "Booked", 1 "Definitely Coming" and 1 "Can't Make It"
