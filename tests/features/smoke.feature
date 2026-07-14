Feature: Application smoke test
  As the delivery team
  I want the walking skeleton to boot and respond
  So that every later feature has a running system to build on

  @phase0 @smoke
  Scenario: The API reports healthy
    Given the CycleSync system is running
    When I request the health endpoint
    Then the response status is 200
    And the health status is "Healthy"

  @phase0 @smoke @ui
  Scenario: The SPA shell loads
    Given the CycleSync system is running
    When I open the application in a browser
    Then I see the CycleSync application shell
