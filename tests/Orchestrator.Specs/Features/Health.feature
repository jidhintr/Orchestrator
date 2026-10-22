@UC-000
Feature: Platform health and request pipeline

  Scenario: Health is reported when PostgreSQL is reachable
    When I send a GET request to "/api/health"
    Then the response status is 200
    And the response body is "Healthy"

  Scenario: Health reports unhealthy, never a false success, when the database is unreachable
    Given the database is unreachable
    When I send a GET request to "/api/health"
    Then the response status is 503
    And the response body is "Unhealthy"

  Scenario: The caller's correlation id is echoed back
    Given I use the correlation id "test-corr-123"
    When I send a GET request to "/api/health"
    Then the response header "X-Correlation-Id" is "test-corr-123"

  Scenario: A correlation id is generated when none is supplied
    When I send a GET request to "/api/health"
    Then the response header "X-Correlation-Id" is present

  Scenario: Liveness stays healthy while the database is down, so the UI can tell API down from database down
    Given the database is unreachable
    When I send a GET request to "/api/health/live"
    Then the response status is 200
    And the response body is "Healthy"

  Scenario: Readiness is healthy when PostgreSQL is reachable
    When I send a GET request to "/api/health/ready"
    Then the response status is 200
    And the response body is "Healthy"

  Scenario: Readiness reports not ready, never a false success, when the database is unreachable
    Given the database is unreachable
    When I send a GET request to "/api/health/ready"
    Then the response status is 503
    And the response body is "Unhealthy"

  Scenario Outline: Health endpoints need no sign-in
    Given I act as nobody
    When I send a GET request to "<path>"
    Then the response status is 200

    Examples:
      | path              |
      | /api/health       |
      | /api/health/live  |
      | /api/health/ready |
