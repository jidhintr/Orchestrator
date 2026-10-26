@UC-009
Feature: Order approvals and the assembly line

  An order is approved stage by stage by the departments of its approval chain, then its job steps run one after the other.
  Job steps belong to the Operations department. Employees assign only themselves; managers assign anyone in the department.

  Background:
    Given a client and a catalog exist
    And a user "Acc" exists in the departments "ACCOUNTING" with the roles "Manager"
    And a user "Opa" exists in the departments "OPERATIONS" with the roles "Manager"
    And a user "Eve" exists in the departments "OPERATIONS" with the roles ""
    And an order is placed for that client and catalog

  Scenario: The approving department manager starts the assembly line
    Given I act as "Acc"
    When I send a POST request to "/api/v1/orders/{order:current}/approvals/approve"
    Then the response status is 200
    And the response JSON "status" is "InProgress"
    When I send a GET request to "/api/v1/orders/{order:current}/approvals"
    Then the response list contains an item where "decision" is "Approved"
    Given I remember step 1 of the order as "first"
    And I act as "Opa"
    When I send a GET request to "/api/v1/order-steps/{step:first}"
    Then the response JSON "status" is "Ready"

  Scenario: A denial cancels the order back to Estimation
    Given I act as "Acc"
    When I send a POST request to "/api/v1/orders/{order:current}/approvals/deny" with body:
      """
      { "reason": "Credit limit exceeded" }
      """
    Then the response status is 200
    And the response JSON "status" is "Cancelled"
    And the response JSON "returnedToDepartmentId" is "8"

  Scenario: A manager of another department cannot approve
    Given I act as "Opa"
    When I send a POST request to "/api/v1/orders/{order:current}/approvals/approve"
    Then the response is a problem with status 403

  Scenario: An employee assigns themselves, starts and completes a step and the next one becomes ready
    Given the order is approved
    And I remember step 1 of the order as "first"
    And I remember step 2 of the order as "second"
    And I act as "Eve"
    When I send a POST request to "/api/v1/order-steps/{step:first}/assign" with body:
      """
      { "userId": {user:Eve} }
      """
    Then the response status is 200
    When I send a POST request to "/api/v1/order-steps/{step:first}/start"
    Then the response JSON "status" is "InProgress"
    When I send a POST request to "/api/v1/order-steps/{step:first}/progress" with body:
      """
      { "progress": 60, "note": "Cutting" }
      """
    Then the response JSON "progress" is "60"
    When I send a POST request to "/api/v1/order-steps/{step:first}/complete"
    Then the response JSON "status" is "Completed"
    When I send a GET request to "/api/v1/order-steps/{step:second}"
    Then the response JSON "status" is "Ready"
    When I send a GET request to "/api/v1/order-steps/{step:first}/logs"
    Then the response list contains an item where "entryType" is "Completed"

  Scenario: An employee cannot assign a colleague but a manager can
    Given the order is approved
    And I remember step 1 of the order as "first"
    And I act as "Eve"
    When I send a POST request to "/api/v1/order-steps/{step:first}/assign" with body:
      """
      { "userId": {user:Opa} }
      """
    Then the response is a problem with status 403
    Given I act as "Opa"
    When I send a POST request to "/api/v1/order-steps/{step:first}/assign" with body:
      """
      { "userId": {user:Eve} }
      """
    Then the response status is 200

  Scenario: A step on hold needs a reason and can be resumed
    Given the order is approved
    And I remember step 1 of the order as "first"
    And I act as "Opa"
    When I send a POST request to "/api/v1/order-steps/{step:first}/start"
    And I send a POST request to "/api/v1/order-steps/{step:first}/hold" with body:
      """
      { "reason": "" }
      """
    Then the response is a problem with status 400
    When I send a POST request to "/api/v1/order-steps/{step:first}/hold" with body:
      """
      { "reason": "Machine fault" }
      """
    Then the response JSON "status" is "OnHold"
    When I send a POST request to "/api/v1/order-steps/{step:first}/resume"
    Then the response JSON "status" is "InProgress"

  Scenario: A later step rejects the work back to the previous one
    Given the order is approved
    And I remember step 1 of the order as "first"
    And I remember step 2 of the order as "second"
    And I act as "Opa"
    When I send a POST request to "/api/v1/order-steps/{step:first}/start"
    And I send a POST request to "/api/v1/order-steps/{step:first}/complete"
    And I send a POST request to "/api/v1/order-steps/{step:second}/start"
    And I send a POST request to "/api/v1/order-steps/{step:second}/reject" with body:
      """
      { "reason": "Scratched surface" }
      """
    Then the response status is 200
    And the response JSON "status" is "Waiting"
    When I send a GET request to "/api/v1/order-steps/{step:first}"
    Then the response JSON "status" is "Ready"

  Scenario: The first step cannot reject
    Given the order is approved
    And I remember step 1 of the order as "first"
    And I act as "Opa"
    When I send a POST request to "/api/v1/order-steps/{step:first}/start"
    And I send a POST request to "/api/v1/order-steps/{step:first}/reject" with body:
      """
      { "reason": "Nothing before" }
      """
    Then the response is a problem with status 409

  Scenario: The work queue shows ready steps and is closed to clients
    Given the order is approved
    And I remember step 1 of the order as "first"
    And I act as "Eve"
    When I send a GET request to "/api/v1/order-steps?status=Ready"
    Then the response list contains an item where "id" is "{step:first}"
    Given a client user "Cli" exists
    And I act as "Cli"
    When I send a GET request to "/api/v1/order-steps"
    Then the response is a problem with status 403

  Scenario: A step of a cancelled order cannot change
    Given the order is approved
    And I remember step 1 of the order as "first"
    When I send a POST request to "/api/v1/orders/{order:current}/cancel" with body:
      """
      { "reason": "Client request" }
      """
    Given I act as "Opa"
    When I send a POST request to "/api/v1/order-steps/{step:first}/start"
    Then the response is a problem with status 409
