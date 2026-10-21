@UC-003
Feature: Role management and department roles

  Roles are managed by admins and super admins only (acting user in the X-Acting-User-Id header, temporary until
  authentication). Departments carry roles that all their users inherit. Reading is open.

  Background:
    Given a department "Press {unique}" with code "PRS{unique}" exists
    And a user "Ann" exists in the departments "PRS{unique}" with the roles ""

  Scenario: Seeded system roles exist after migration
    When I send a GET request to "/api/v1/roles"
    Then the response status is 200
    And the response list contains an item where "name" is "Super Admin"
    And the response list contains an item where "name" is "Admin"
    And the response list contains an item where "name" is "Manager"
    And the response list contains an item where "name" is "Employee"
    And the response list contains an item where "name" is "Client"

  Scenario: Create, update, disable, enable and delete a role
    Given I act as "Admin"
    When I send a POST request to "/api/v1/roles" with body:
      """
      { "name": "Estimator {unique}", "description": "Prepares estimates" }
      """
    Then the response status is 201
    And the response header "Location" is present
    And the response JSON "isSystem" is "false"
    And the response JSON "isActive" is "true"
    When I send a PUT request to "/api/v1/roles/{id}" with body:
      """
      { "name": "Estimator {unique}", "description": "Prepares and revises estimates" }
      """
    Then the response status is 200
    And the response JSON "description" is "Prepares and revises estimates"
    When I send a POST request to "/api/v1/roles/{id}/disable"
    Then the response status is 204
    When I send a GET request to "/api/v1/roles/{id}"
    Then the response JSON "isActive" is "false"
    When I send a POST request to "/api/v1/roles/{id}/enable"
    Then the response status is 204
    When I send a DELETE request to "/api/v1/roles/{id}"
    Then the response status is 204
    When I send a GET request to "/api/v1/roles/{id}"
    Then the response is a problem with status 404

  Scenario: A disabled role cannot be assigned
    Given a role "Estimator {unique}" exists
    And I act as "Admin"
    When I send a POST request to "/api/v1/roles/{role:Estimator {unique}}/disable"
    Then the response status is 204
    When I send a PUT request to "/api/v1/users/{user:Ann}/roles" with body:
      """
      { "roles": ["Estimator {unique}"] }
      """
    Then the response is a problem with status 400

  Scenario Outline: System roles are protected
    Given I act as "SA"
    When I send a <method> request to "/api/v1/roles/{role:Admin}<suffix>" with body:
      """
      { "name": "Renamed {unique}", "description": "x" }
      """
    Then the response is a problem with status 409

    Examples:
      | method | suffix   |
      | PUT    |          |
      | POST   | /disable |
      | DELETE |          |

  Scenario Outline: Duplicate or invalid role input is rejected
    Given I act as "Admin"
    When I send a POST request to "/api/v1/roles" with body:
      """
      { "name": "<name>", "description": "<description>" }
      """
    Then the response status is <status>

    Examples:
      | case                  | name    | description | status |
      | same name, other case | admin   | Duplicate   | 409    |
      | name too short        | x       | Short       | 400    |

  Scenario: A role that is still assigned cannot be deleted
    Given a role "Estimator {unique}" exists
    And I act as "Admin"
    When I send a PUT request to "/api/v1/users/{user:Ann}/roles" with body:
      """
      { "roles": ["Estimator {unique}"] }
      """
    Then the response status is 200
    When I send a DELETE request to "/api/v1/roles/{role:Estimator {unique}}"
    Then the response is a problem with status 409
    When I send a PUT request to "/api/v1/users/{user:Ann}/roles" with body:
      """
      { "roles": [] }
      """
    And I send a DELETE request to "/api/v1/roles/{role:Estimator {unique}}"
    Then the response status is 204

  Scenario: A role listing shows users who hold it directly or through their department
    Given a role "Estimator {unique}" exists
    And the department with code "PRS{unique}" has the roles "Estimator {unique}"
    And a user "Ben" exists in the departments "UNASSIGNED" with the roles "Estimator {unique}"
    When I send a GET request to "/api/v1/roles/{role:Estimator {unique}}/users"
    Then the response status is 200
    And the response list contains an item where "firstName" is "Ann"
    And the response list contains an item where "firstName" is "Ben"

  Scenario: The Clients department carries the Client role
    When I send a GET request to "/api/v1/departments/{dept:CLIENTS}/roles"
    Then the response status is 200
    And the response list contains an item where "name" is "Client"

  Scenario: An admin sets the roles of a department and its users inherit them
    Given I act as "Admin"
    When I send a PUT request to "/api/v1/departments/{dept:PRS{unique}}/roles" with body:
      """
      { "roles": ["Employee"] }
      """
    Then the response status is 200
    And the response list contains an item where "name" is "Employee"
    When I send a GET request to "/api/v1/users/{user:Ann}"
    Then the response JSON list "departmentRoles" has an item "Employee"
    When I send a PUT request to "/api/v1/departments/{dept:PRS{unique}}/roles" with body:
      """
      { "roles": ["Wizard"] }
      """
    Then the response is a problem with status 400

  Scenario Outline: Only admins and super admins can change roles and departments
    Given I act as "<actor>"
    When I send a <method> request to "<path>" with body:
      """
      {}
      """
    Then the response is a problem with status 403

    Examples:
      | actor | method | path                                       |
      | Ann   | POST   | /api/v1/roles                              |
      | Ann   | PUT    | /api/v1/roles/{role:Employee}              |
      | Ann   | POST   | /api/v1/roles/{role:Employee}/disable      |
      | Ann   | POST   | /api/v1/roles/{role:Employee}/enable       |
      | Ann   | DELETE | /api/v1/roles/{role:Employee}              |
      | Ann   | POST   | /api/v1/departments                        |
      | Ann   | PUT    | /api/v1/departments/{dept:QA}              |
      | Ann   | POST   | /api/v1/departments/{dept:QA}/deactivate   |
      | Ann   | PUT    | /api/v1/departments/{dept:QA}/roles        |

  Scenario: Reading roles and departments needs no acting user
    Given I act as nobody
    When I send a GET request to "/api/v1/roles"
    Then the response status is 200
    When I send a GET request to "/api/v1/departments"
    Then the response status is 200

  Scenario: Changing roles without an acting user is unauthorized
    Given I act as nobody
    When I send a POST request to "/api/v1/roles" with body:
      """
      { "name": "Ghost {unique}", "description": "x" }
      """
    Then the response is a problem with status 401
