@UC-006
Feature: Approval chains

  One default chain always exists and is followed by every catalog without its own chain. Admins and super admins edit it
  and may give a single catalog its own chain. Reading needs a signed-in user.

  Background:
    Given a department "Press {unique}" with code "PRS{unique}" exists
    And a user "Ann" exists in the departments "PRS{unique}" with the roles ""

  Scenario: The default chain exists after migration
    When I send a GET request to "/api/v1/approval-chains"
    Then the response status is 200
    And the response list contains an item where "isDefault" is "true"

  Scenario: Edit the default chain
    When I send a PUT request to "/api/v1/approval-chains/1" with body:
      """
      { "name": "Default", "description": "Company wide", "departmentIds": [{dept:ACCOUNTING}, {dept:QUALITY_CHECK}, {dept:ADMINISTRATIVES}] }
      """
    Then the response status is 200
    And the response JSON "isDefault" is "true"
    And the approval chain departments are "Accounting,Quality Check,Administratives"
    When I send a PUT request to "/api/v1/approval-chains/1" with body:
      """
      { "name": "Default", "departmentIds": [{dept:QUALITY_CHECK}, {dept:ACCOUNTING}] }
      """
    Then the approval chain departments are "Quality Check,Accounting"

  Scenario: A catalog follows the default chain until it gets its own
    Given a catalog with SKU "PRM{unique}" exists
    When I send a GET request to "/api/v1/approval-chains/for-catalog/{catalog:current}"
    Then the response status is 200
    And the response JSON "isDefault" is "true"
    When I send a POST request to "/api/v1/approval-chains" with body:
      """
      { "name": "Premium {unique}", "catalogId": {catalog:current}, "departmentIds": [{dept:ACCOUNTING}, {dept:PRS{unique}}] }
      """
    Then the response status is 201
    And the response header "Location" is present
    And the response JSON "isDefault" is "false"
    And the approval chain departments are "Accounting,Press {unique}"
    When I send a GET request to "/api/v1/approval-chains/for-catalog/{catalog:current}"
    Then the response JSON "isDefault" is "false"
    And the approval chain departments are "Accounting,Press {unique}"

  Scenario: Deleting a catalog chain sends the catalog back to the default chain
    Given a catalog with SKU "AUD{unique}" exists
    And an approval chain for the catalog with the departments "ACCOUNTING" exists
    When I send a DELETE request to "/api/v1/approval-chains/{id}"
    Then the response status is 204
    When I send a GET request to "/api/v1/approval-chains/for-catalog/{catalog:current}"
    Then the response JSON "isDefault" is "true"

  Scenario: The default chain cannot be deleted
    When I send a DELETE request to "/api/v1/approval-chains/1"
    Then the response is a problem with status 409

  Scenario: A catalog has at most one own chain
    Given a catalog with SKU "ONE{unique}" exists
    And an approval chain for the catalog with the departments "ACCOUNTING" exists
    When I send a POST request to "/api/v1/approval-chains" with body:
      """
      { "name": "Second {unique}", "catalogId": {catalog:current}, "departmentIds": [{dept:QUALITY_CHECK}] }
      """
    Then the response is a problem with status 409

  Scenario: Invalid chains are rejected
    Given a catalog with SKU "BAD{unique}" exists
    When I send a POST request to "/api/v1/approval-chains" with body:
      """
      { "name": "X", "catalogId": {id}, "departmentIds": [] }
      """
    Then the response is a problem with status 400
    When I send a POST request to "/api/v1/approval-chains" with body:
      """
      { "name": "Dup {unique}", "catalogId": {id}, "departmentIds": [{dept:QUALITY_CHECK}, {dept:QUALITY_CHECK}] }
      """
    Then the response is a problem with status 400
    When I send a POST request to "/api/v1/approval-chains" with body:
      """
      { "name": "Ghost {unique}", "catalogId": {id}, "departmentIds": [999999] }
      """
    Then the response is a problem with status 400
    When I send a POST request to "/api/v1/approval-chains" with body:
      """
      { "name": "NoCatalog {unique}", "catalogId": 999999, "departmentIds": [{dept:QUALITY_CHECK}] }
      """
    Then the response is a problem with status 400

  Scenario: Unknown chain or catalog returns 404
    When I send a GET request to "/api/v1/approval-chains/999999"
    Then the response is a problem with status 404
    When I send a GET request to "/api/v1/approval-chains/for-catalog/999999"
    Then the response is a problem with status 404

  Scenario Outline: Only admins and super admins can change approval chains
    Given I act as "Ann"
    When I send a <method> request to "<path>" with body:
      """
      {}
      """
    Then the response is a problem with status 403

    Examples:
      | method | path                         |
      | POST   | /api/v1/approval-chains      |
      | PUT    | /api/v1/approval-chains/1    |
      | DELETE | /api/v1/approval-chains/1    |

  Scenario: Changing a chain without an acting user is unauthorized
    Given I act as nobody
    When I send a PUT request to "/api/v1/approval-chains/1" with body:
      """
      { "name": "Default", "departmentIds": [{dept:QUALITY_CHECK}] }
      """
    Then the response is a problem with status 401
