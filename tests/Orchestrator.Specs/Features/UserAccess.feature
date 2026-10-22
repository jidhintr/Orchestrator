@UC-002
Feature: Users, departments, roles and who may change whom

  The acting user is named in the X-Acting-User-Id header (temporary, until authentication). Rank: Super Admin > Admin >
  Manager > Employee. Nobody edits a user at or above their own rank (a Super Admin may edit anyone); a Manager only
  works with users of their own departments. A user's roles are the roles of their departments plus their own extras.

  Background:
    Given a department "Press {unique}" with code "PRS{unique}" exists
    And the department with code "PRS{unique}" has the roles "Employee"
    And a department "Bindery {unique}" with code "BND{unique}" exists
    And the department with code "BND{unique}" has the roles "Employee"
    And a user "Ann" exists in the departments "PRS{unique}" with the roles "Manager"
    And a user "Bob" exists in the departments "BND{unique}" with the roles "Manager"
    And a user "Cal" exists in the departments "PRS{unique}" with the roles ""
    And a user "Dee" exists in the departments "BND{unique}" with the roles ""

  Scenario: Admin creates an employee who lands in the Unassigned department by default
    Given I act as "Admin"
    When I send a POST request to "/api/v1/users" with body:
      """
      { "firstName": "Eli", "lastName": "Nash", "mobile": "9876543210" }
      """
    Then the response status is 201
    And the response JSON list "departments" has an item "Unassigned"
    And the response JSON list "roles" has no item "Manager"

  Scenario: Admin creates an employee in several departments at once
    Given I act as "Admin"
    When I send a POST request to "/api/v1/users" with body:
      """
      { "firstName": "Eli", "lastName": "Nash", "mobile": "9876543210", "departmentIds": [{dept:PRS{unique}}, {dept:BND{unique}}] }
      """
    Then the response status is 201
    And the response JSON list "departments" has an item "Press {unique}"
    And the response JSON list "departments" has an item "Bindery {unique}"
    And the response JSON list "departments" has no item "Unassigned"
    And the response JSON list "departmentRoles" has an item "Employee"

  Scenario: Admin sets an employee's departments and gives them several extra roles
    Given a role "Estimator {unique}" exists
    And I act as "Admin"
    When I send a PUT request to "/api/v1/users/{user:Cal}/departments" with body:
      """
      { "departmentIds": [{dept:PRS{unique}}, {dept:BND{unique}}] }
      """
    Then the response status is 200
    And the response JSON list "departments" has an item "Press {unique}"
    And the response JSON list "departments" has an item "Bindery {unique}"
    When I send a PUT request to "/api/v1/users/{user:Cal}/roles" with body:
      """
      { "roles": ["Manager", "Estimator {unique}"] }
      """
    Then the response status is 200
    And the response JSON list "roles" has an item "Manager"
    And the response JSON list "roles" has an item "Estimator {unique}"

  Scenario: Admin makes an employee a manager, who can then add employees to their department
    Given I act as "Admin"
    When I send a PUT request to "/api/v1/users/{user:Cal}/roles" with body:
      """
      { "roles": ["Manager"] }
      """
    Then the response status is 200
    Given I act as "Cal"
    When I send a POST request to "/api/v1/users" with body:
      """
      { "firstName": "Eli", "lastName": "Nash", "mobile": "9876543210" }
      """
    Then the response status is 201
    And the response JSON list "departments" has an item "Press {unique}"

  Scenario Outline: An admin cannot create or promote admins or super admins
    Given I act as "Admin"
    When I send a PUT request to "/api/v1/users/{user:Cal}/roles" with body:
      """
      { "roles": ["<role>"] }
      """
    Then the response is a problem with status 403
    When I send a POST request to "/api/v1/users" with body:
      """
      { "firstName": "Eli", "lastName": "Nash", "mobile": "9876543210", "roles": ["<role>"] }
      """
    Then the response is a problem with status 403

    Examples:
      | role        |
      | Admin       |
      | Super Admin |

  Scenario: An admin cannot put anyone into a department that carries the Admin role
    Given I act as "Admin"
    When I send a POST request to "/api/v1/users" with body:
      """
      { "firstName": "Eli", "lastName": "Nash", "mobile": "9876543210", "departmentIds": [{dept:ADMIN}] }
      """
    Then the response is a problem with status 403
    When I send a PUT request to "/api/v1/users/{user:Cal}/departments" with body:
      """
      { "departmentIds": [{dept:ADMIN}] }
      """
    Then the response is a problem with status 403

  Scenario: A super admin can create and edit admins
    Given I act as "SA"
    When I send a POST request to "/api/v1/users" with body:
      """
      { "firstName": "Eve", "lastName": "Root", "mobile": "9876543210", "roles": ["Admin"] }
      """
    Then the response status is 201
    And the response JSON list "roles" has an item "Admin"
    When I send a PUT request to "/api/v1/users/{id}" with body:
      """
      { "firstName": "Eve", "lastName": "Rooter", "mobile": "9876543210", "isActive": true }
      """
    Then the response status is 200
    And the response JSON "lastName" is "Rooter"

  Scenario Outline: An admin cannot edit admins or super admins
    Given I act as "Admin"
    When I send a PUT request to "/api/v1/users/<target>" with body:
      """
      { "firstName": "Zed", "lastName": "Tester", "mobile": "9876543210", "isActive": true }
      """
    Then the response is a problem with status 403

    Examples:
      | target        |
      | {user:Admin}  |
      | {user:SA}     |

  Scenario: A manager creates an employee in their own department by default
    Given I act as "Ann"
    When I send a POST request to "/api/v1/users" with body:
      """
      { "firstName": "Eli", "lastName": "Nash", "mobile": "9876543210" }
      """
    Then the response status is 201
    And the response JSON list "departments" has an item "Press {unique}"
    And the response JSON list "departments" has no item "Bindery {unique}"
    And the response JSON list "departmentRoles" has an item "Employee"

  Scenario: A manager cannot create an employee in another department
    Given I act as "Ann"
    When I send a POST request to "/api/v1/users" with body:
      """
      { "firstName": "Eli", "lastName": "Nash", "mobile": "9876543210", "departmentIds": [{dept:BND{unique}}] }
      """
    Then the response is a problem with status 403

  Scenario Outline: A manager cannot create managers, admins or super admins
    Given I act as "Ann"
    When I send a POST request to "/api/v1/users" with body:
      """
      { "firstName": "Eli", "lastName": "Nash", "mobile": "9876543210", "roles": ["<role>"] }
      """
    Then the response is a problem with status 403

    Examples:
      | role        |
      | Manager     |
      | Admin       |
      | Super Admin |

  Scenario: A manager unassigns an employee from their department and the employee waits in Unassigned
    Given I act as "Ann"
    When I send a PUT request to "/api/v1/users/{user:Cal}/departments" with body:
      """
      { "departmentIds": [] }
      """
    Then the response status is 200
    And the response JSON list "departments" has an item "Unassigned"
    And the response JSON list "departments" has no item "Press {unique}"
    When I send a PUT request to "/api/v1/users/{user:Cal}/roles" with body:
      """
      { "roles": [] }
      """
    Then the response is a problem with status 403

  Scenario: An employee can belong to several departments and a manager removes only their own
    Given I act as "Admin"
    When I send a PUT request to "/api/v1/users/{user:Cal}/departments" with body:
      """
      { "departmentIds": [{dept:PRS{unique}}, {dept:BND{unique}}] }
      """
    Then the response status is 200
    Given I act as "Ann"
    When I send a PUT request to "/api/v1/users/{user:Cal}/departments" with body:
      """
      { "departmentIds": [] }
      """
    Then the response status is 200
    And the response JSON list "departments" has an item "Bindery {unique}"
    And the response JSON list "departments" has no item "Press {unique}"
    And the response JSON list "departments" has no item "Unassigned"

  Scenario: A manager can assign only roles that belong to their own department
    Given a role "Estimator {unique}" exists
    And I act as "Ann"
    When I send a PUT request to "/api/v1/users/{user:Cal}/roles" with body:
      """
      { "roles": ["Employee"] }
      """
    Then the response status is 200
    And the response JSON list "roles" has an item "Employee"
    When I send a PUT request to "/api/v1/users/{user:Cal}/roles" with body:
      """
      { "roles": ["Estimator {unique}"] }
      """
    Then the response is a problem with status 403
    When I send a PUT request to "/api/v1/users/{user:Cal}/roles" with body:
      """
      { "roles": ["Manager"] }
      """
    Then the response is a problem with status 403

  Scenario Outline: A manager cannot edit users outside their department, peers, admins or themselves
    Given I act as "Ann"
    When I send a PUT request to "/api/v1/users/<target>" with body:
      """
      { "firstName": "Zed", "lastName": "Tester", "mobile": "9876543210", "isActive": true }
      """
    Then the response is a problem with status 403

    Examples:
      | target        |
      | {user:Dee}    |
      | {user:Bob}    |
      | {user:Ann}    |
      | {user:Admin}  |
      | {user:SA}     |

  Scenario: A manager gets the department's roles plus their own extra role
    When I send a GET request to "/api/v1/users/{user:Ann}"
    Then the response status is 200
    And the response JSON list "departmentRoles" has an item "Employee"
    And the response JSON list "roles" has an item "Manager"
    And the response JSON list "roles" has no item "Employee"

  Scenario Outline: An employee cannot change anyone, including themselves
    Given I act as "Cal"
    When I send a PUT request to "/api/v1/users/<target>" with body:
      """
      { "firstName": "Zed", "lastName": "Tester", "mobile": "9876543210", "isActive": true }
      """
    Then the response is a problem with status 403
    When I send a PUT request to "/api/v1/users/<target>/roles" with body:
      """
      { "roles": [] }
      """
    Then the response is a problem with status 403

    Examples:
      | target        |
      | {user:Cal}    |
      | {user:Dee}    |
      | {user:Ann}    |
      | {user:Admin}  |
      | {user:SA}     |

  Scenario: An employee cannot create users
    Given I act as "Cal"
    When I send a POST request to "/api/v1/users" with body:
      """
      { "firstName": "Eli", "lastName": "Nash", "mobile": "9876543210" }
      """
    Then the response is a problem with status 403

  Scenario: Users of the Clients department inherit the Client role and can change nobody
    Given a user "Cli" exists in the departments "CLIENTS" with the roles ""
    When I send a GET request to "/api/v1/users/{user:Cli}"
    Then the response JSON list "departmentRoles" has an item "Client"
    And the response JSON list "roles" has no item "Client"
    Given I act as "Cli"
    When I send a PUT request to "/api/v1/users/{user:Cal}" with body:
      """
      { "firstName": "Zed", "lastName": "Tester", "mobile": "9876543210", "isActive": true }
      """
    Then the response is a problem with status 403

  Scenario Outline: Requests without a valid acting user are unauthorized
    Given <actor>
    When I send a POST request to "/api/v1/users" with body:
      """
      { "firstName": "Eli", "lastName": "Nash", "mobile": "9876543210" }
      """
    Then the response is a problem with status 401

    Examples:
      | actor                  |
      | I act as nobody        |
      | I act as "999999"      |
      | I act as "not-a-number"|

  Scenario: A deactivated user can no longer act
    Given I act as "SA"
    When I send a PUT request to "/api/v1/users/{user:Ann}" with body:
      """
      { "firstName": "Ann", "lastName": "Tester", "mobile": "9876543210", "isActive": false }
      """
    Then the response status is 200
    Given I act as "Ann"
    When I send a POST request to "/api/v1/users" with body:
      """
      { "firstName": "Eli", "lastName": "Nash", "mobile": "9876543210" }
      """
    Then the response is a problem with status 401

  Scenario Outline: Unknown departments and roles are rejected
    Given I act as "Admin"
    When I send a PUT request to "/api/v1/users/{user:Cal}/<what>" with body:
      """
      <body>
      """
    Then the response is a problem with status 400

    Examples:
      | what        | body                          |
      | departments | { "departmentIds": [999999] } |
      | roles       | { "roles": ["Wizard"] }       |

  Scenario Outline: Invalid user input is rejected
    Given I act as "Admin"
    When I send a POST request to "/api/v1/users" with body:
      """
      { "firstName": "<first>", "lastName": "<last>", "mobile": "<mobile>", "email": "<email>" }
      """
    Then the response status is 400

    Examples:
      | case             | first | last | mobile     | email          |
      | first too short  | E     | Nash | 9876543210 | e@example.com  |
      | last too long    | Eli   | Nashhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh | 9876543210 | e@example.com |
      | bad mobile       | Eli   | Nash | abc        | e@example.com  |
      | bad email        | Eli   | Nash | 9876543210 | not-an-email   |

  Scenario: Email is optional but unique when given
    Given I act as "Admin"
    When I send a POST request to "/api/v1/users" with body:
      """
      { "firstName": "Eli", "lastName": "Nash", "mobile": "9876543210", "email": "eli.{unique}@example.com" }
      """
    Then the response status is 201
    When I send a POST request to "/api/v1/users" with body:
      """
      { "firstName": "Eli", "lastName": "Two", "mobile": "9876543210", "email": "ELI.{unique}@example.com" }
      """
    Then the response is a problem with status 409
    When I send a POST request to "/api/v1/users" with body:
      """
      { "firstName": "Eli", "lastName": "Three", "mobile": "9876543210" }
      """
    Then the response status is 201

  Scenario: Unknown users are not found
    Given I act as "Admin"
    When I send a GET request to "/api/v1/users/999999"
    Then the response is a problem with status 404
    When I send a PUT request to "/api/v1/users/999999/roles" with body:
      """
      { "roles": [] }
      """
    Then the response is a problem with status 404
