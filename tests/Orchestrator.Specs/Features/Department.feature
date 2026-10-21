@UC-001
Feature: Department management

  Scenario: Seeded system departments exist after migration
    When I send a GET request to "/api/v1/departments"
    Then the response status is 200
    And the response list contains the departments:
      | code        | name        |
      | SUPER_ADMIN | Super Admin |
      | ADMIN       | Admin       |
      | ACCOUNTS    | Accounts    |
      | QA          | QA          |
      | DELIVERY    | Delivery    |
      | CLIENTS     | Clients     |

  Scenario: Create a department and read it back
    When I send a POST request to "/api/v1/departments" with body:
      """
      { "name": "Cutting {unique}", "code": "CUT{unique}", "description": "Cuts printed sheets" }
      """
    Then the response status is 201
    And the response header "Location" is present
    And the response JSON "name" is "Cutting {unique}"
    And the response JSON "code" is "CUT{unique}"
    And the response JSON "isActive" is "true"
    And the response JSON "isSystem" is "false"
    And the response JSON "id" is at least 1000
    When I send a GET request to "/api/v1/departments/{id}"
    Then the response status is 200
    And the response JSON "description" is "Cuts printed sheets"
    When I send a GET request to "/api/v1/departments"
    Then the response list contains an item where "code" is "CUT{unique}"

  Scenario: Update the name and description of a department
    Given a department "Binding {unique}" with code "BND{unique}" exists
    When I send a PUT request to "/api/v1/departments/{id}" with body:
      """
      { "name": "Finishing {unique}", "description": "Binds and finishes" }
      """
    Then the response status is 200
    And the response JSON "name" is "Finishing {unique}"
    And the response JSON "code" is "BND{unique}"
    When I send a GET request to "/api/v1/departments/{id}"
    Then the response JSON "name" is "Finishing {unique}"
    And the response JSON "description" is "Binds and finishes"

  Scenario: Deactivate a department
    Given a department "Folding {unique}" with code "FLD{unique}" exists
    When I send a POST request to "/api/v1/departments/{id}/deactivate"
    Then the response status is 204
    When I send a GET request to "/api/v1/departments/{id}"
    Then the response JSON "isActive" is "false"

  Scenario Outline: Invalid input is rejected and nothing is saved
    When I send a POST request to "/api/v1/departments" with body:
      """
      { "name": "<name>", "code": "<code>" }
      """
    Then the response status is 400
    And the response is a problem with status 400
    When I send a GET request to "/api/v1/departments"
    Then the response list does not contain an item where "code" is "<code>"

    Examples:
      | case              | name           | code         |
      | missing name      |                | BAD1{unique} |
      | lowercase code    | Bad {unique}   | bad2{unique} |
      | code with spaces  | Bad {unique}   | BAD 3        |
      | code too short    | Bad {unique}   | B            |

  Scenario: Invalid update is rejected and the department is unchanged
    Given a department "Mailing {unique}" with code "MAL{unique}" exists
    When I send a PUT request to "/api/v1/departments/{id}" with body:
      """
      { "name": "" }
      """
    Then the response status is 400
    When I send a GET request to "/api/v1/departments/{id}"
    Then the response JSON "name" is "Mailing {unique}"

  Scenario Outline: Duplicate names or codes are rejected
    Given a department "Packing {unique}" with code "PCK{unique}" exists
    When I send a POST request to "/api/v1/departments" with body:
      """
      { "name": "<name>", "code": "<code>" }
      """
    Then the response is a problem with status 409

    Examples:
      | case                  | name                 | code          |
      | same code             | Other {unique}       | PCK{unique}   |
      | same name, other case | packing {unique}     | PCK2{unique}  |

  Scenario Outline: Unknown departments are not found
    When I send a <method> request to "/api/v1/departments/999999<suffix>"
    Then the response is a problem with status 404

    Examples:
      | method | suffix      |
      | GET    |             |
      | POST   | /deactivate |

  Scenario: Updating an unknown department is not found
    When I send a PUT request to "/api/v1/departments/999999" with body:
      """
      { "name": "Ghost {unique}" }
      """
    Then the response is a problem with status 404

  Scenario: System departments cannot be renamed
    Given I have the id of the department with code "QA"
    When I send a PUT request to "/api/v1/departments/{id}" with body:
      """
      { "name": "Quality {unique}" }
      """
    Then the response is a problem with status 409
    When I send a GET request to "/api/v1/departments/{id}"
    Then the response JSON "name" is "QA"

  Scenario: System departments cannot be deactivated
    Given I have the id of the department with code "ADMIN"
    When I send a POST request to "/api/v1/departments/{id}/deactivate"
    Then the response is a problem with status 409
    When I send a GET request to "/api/v1/departments/{id}"
    Then the response JSON "isActive" is "true"

  Scenario: Unexpected failures return a standard error without internal details
    Given the database is unreachable
    When I send a GET request to "/api/v1/departments"
    Then the response is a problem with status 500
    And the response JSON "title" is "An unexpected error occurred."
    And the response JSON "correlationId" is present
    And the response body does not contain "Npgsql"
    And the response body does not contain "localhost"

  Scenario: Only version 1 of the Department API exists; unknown versions are not found
    When I send a GET request to "/api/v2/departments"
    Then the response is a problem with status 404
    When I send a GET request to "/api/v1/departments"
    Then the response header "api-supported-versions" is "1.0"

  Scenario: Department endpoints require an explicit version
    When I send a GET request to "/api/departments"
    Then the response status is 404
