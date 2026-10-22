@UC-007
Feature: Job type management

  Job types are the assembly-line steps that catalogs are built from. Admins and super admins change them; reading needs a signed-in user.

  Background:
    Given a department "Press {unique}" with code "PRS{unique}" exists
    And a user "Ann" exists in the departments "PRS{unique}" with the roles ""

  Scenario: Seeded job types exist after migration
    When I send a GET request to "/api/v1/job-types"
    Then the response status is 200
    And the response list contains an item where "name" is "Cutting"
    And the response list contains an item where "name" is "Laminating"
    And the response list contains an item where "name" is "Quality Assurance"
    And the response list contains an item where "name" is "Delivery"

  Scenario: Create a job type with cost overrides and read it back
    When I send a POST request to "/api/v1/job-types" with body:
      """
      { "name": "Binding {unique}", "description": "Binds sheets", "laborCost": 3.5, "commissionPercent": 2, "profitPercent": 15 }
      """
    Then the response status is 201
    And the response header "Location" is present
    And the response JSON "isActive" is "true"
    And the response JSON "laborCost" is "3.5"
    And the response JSON "id" is at least 1000
    When I send a GET request to "/api/v1/job-types/{id}"
    Then the response JSON "name" is "Binding {unique}"
    And the response JSON "profitPercent" is "15.00"

  Scenario: Overrides are optional
    When I send a POST request to "/api/v1/job-types" with body:
      """
      { "name": "Folding {unique}" }
      """
    Then the response status is 201
    And the response JSON "laborCost" is "null"

  Scenario: Update a job type
    Given a job type "Trimming {unique}" exists
    When I send a PUT request to "/api/v1/job-types/{id}" with body:
      """
      { "name": "Trimming and Edging {unique}", "laborCost": 4 }
      """
    Then the response status is 200
    And the response JSON "name" is "Trimming and Edging {unique}"
    And the response JSON "laborCost" is "4"

  Scenario: The catalogs using a job type are listed
    Given a catalog with SKU "USE{unique}" exists
    When I send a GET request to "/api/v1/job-types/1/catalogs"
    Then the response status is 200
    And the response list contains an item where "sku" is "USE{unique}"

  Scenario: Activate a deactivated job type
    Given a job type "Fold {unique}" exists
    When I send a POST request to "/api/v1/job-types/{id}/deactivate"
    And I send a POST request to "/api/v1/job-types/{id}/activate"
    Then the response status is 204
    When I send a GET request to "/api/v1/job-types/{id}"
    Then the response JSON "isActive" is "true"

  Scenario: A deactivated job type cannot be added to a catalog
    Given a job type "Stapling {unique}" exists
    When I send a POST request to "/api/v1/job-types/{id}/deactivate"
    Then the response status is 204
    When I send a GET request to "/api/v1/job-types/{id}"
    Then the response JSON "isActive" is "false"
    When I send a POST request to "/api/v1/catalogs" with body:
      """
      { "sku": "STP{unique}", "name": "Stapled {unique}", "jobTypeIds": [{id}] }
      """
    Then the response is a problem with status 400

  Scenario: Duplicate name is rejected
    Given a job type "Gluing {unique}" exists
    When I send a POST request to "/api/v1/job-types" with body:
      """
      { "name": "gluing {unique}" }
      """
    Then the response is a problem with status 409

  Scenario: Invalid input is rejected
    When I send a POST request to "/api/v1/job-types" with body:
      """
      { "name": "X", "profitPercent": 150, "laborCost": -1 }
      """
    Then the response is a problem with status 400

  Scenario: Unknown job type returns 404
    When I send a GET request to "/api/v1/job-types/999999"
    Then the response is a problem with status 404
    When I send a GET request to "/api/v1/job-types/999999/catalogs"
    Then the response is a problem with status 404

  Scenario Outline: Only admins and super admins can change job types
    Given I act as "Ann"
    When I send a <method> request to "<path>" with body:
      """
      {}
      """
    Then the response is a problem with status 403

    Examples:
      | method | path                           |
      | POST   | /api/v1/job-types              |
      | PUT    | /api/v1/job-types/1            |
      | POST   | /api/v1/job-types/1/deactivate |
      | POST   | /api/v1/job-types/1/activate   |

  Scenario: Changing a job type without an acting user is unauthorized
    Given I act as nobody
    When I send a POST request to "/api/v1/job-types" with body:
      """
      { "name": "Ghost {unique}" }
      """
    Then the response is a problem with status 401
