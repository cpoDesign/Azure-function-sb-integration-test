Feature: Calculator

Simple calculator for adding two numbers

@mytag
Scenario: Message is sent into service bus that stores a message into storage container
    Given I send message to "topic.1" with message id "47feaabe-a088-4ae8-9571-24c645849b34"
    Then I want a bit
    Then I will be able to to get file from storage container "demo" with name "47feaabe-a088-4ae8-9571-24c645849b34.json"
    