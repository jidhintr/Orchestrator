@UC-010
Feature: Catalog categories

  Categories group catalogs (Bags, Boxes, Seasonal) for sales reporting. Admins and super admins change them; reading needs a signed-in user.

  Background:
    Given a user "Ann" exists in the departments "UNASSIGNED" with the roles "Manager"

  Scenario: Create a category and read it back
    When I send a POST request to "/api/v1/categories" with body:
      """
      { "name": "Bags {unique}", "description": "Paper and gift bags" }
      """
    Then the response status is 201
    And the response header "Location" is present
    And the response JSON "isActive" is "true"
    And the response JSON "id" is at least 1000
    When I send a GET request to "/api/v1/categories/{id}"
    Then the response status is 200
    And the response JSON "name" is "Bags {unique}"
    When I send a GET request to "/api/v1/categories"
    Then the response list contains an item where "name" is "Bags {unique}"

  Scenario: Update a category
    Given a catalog category "Boxes {unique}" exists
    When I send a PUT request to "/api/v1/categories/{id}" with body:
      """
      { "name": "Gift boxes {unique}" }
      """
    Then the response status is 200
    And the response JSON "name" is "Gift boxes {unique}"

  Scenario: A duplicate name is rejected case-insensitively
    Given a catalog category "Seasonal {unique}" exists
    When I send a POST request to "/api/v1/categories" with body:
      """
      { "name": "SEASONAL {unique}" }
      """
    Then the response is a problem with status 409

  Scenario: Deactivate and activate a category
    Given a catalog category "Labels {unique}" exists
    When I send a POST request to "/api/v1/categories/{id}/deactivate"
    Then the response status is 204
    When I send a GET request to "/api/v1/categories/{id}"
    Then the response JSON "isActive" is "false"
    When I send a POST request to "/api/v1/categories/{id}/activate"
    Then the response status is 204
    When I send a GET request to "/api/v1/categories/{id}"
    Then the response JSON "isActive" is "true"

  Scenario: Invalid and unknown requests are rejected
    When I send a POST request to "/api/v1/categories" with body:
      """
      { "name": "x" }
      """
    Then the response is a problem with status 400
    When I send a GET request to "/api/v1/categories/999999"
    Then the response is a problem with status 404
    When I send a POST request to "/api/v1/categories/999999/deactivate"
    Then the response is a problem with status 404

  Scenario Outline: Only admins and super admins can change categories
    Given I act as "Ann"
    When I send a <method> request to "<path>" with body:
      """
      {}
      """
    Then the response is a problem with status 403

    Examples:
      | method | path                                   |
      | POST   | /api/v1/categories             |
      | PUT    | /api/v1/categories/1           |
      | POST   | /api/v1/categories/1/deactivate |
      | POST   | /api/v1/categories/1/activate  |
