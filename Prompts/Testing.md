Act as a senior .NET test engineer.

Generate comprehensive unit test cases for an ASP.NET Core Web API application.

Requirements:

1. Test Coverage
- Include positive test cases (valid inputs, expected success response)
- Include negative test cases (invalid inputs, null values, exceptions)
- Include boundary test cases (edge values like min/max dates, empty results)
- Include error handling scenarios (external API failure, cache failure)

2. Layers to Test
- Controller layer (API endpoints)
- Service / Orchestrator layer (business logic)
- Integration points (mock external dependencies such as Redis and external APIs)

3. Testing Framework and Tools
- Use xUnit for test framework
- Use Moq (or similar) for mocking dependencies
- Follow Arrange-Act-Assert pattern

4. Specific Scenarios for Earthquake API
- Valid date range returns earthquake data (200 OK)
- Invalid date range (startDate > endDate) returns BadRequest
- Null or missing parameters handled gracefully
- External API failure handled with proper error logging
- Redis cache HIT returns cached data
- Redis cache MISS calls external API and stores result
- Empty API response handled correctly

5. Assertions
- Verify HTTP status codes
- Verify returned data shape and content
- Verify that mocked dependencies are called correctly
- Verify logging (optional, if ILogger mocked)

6. Structure
- Separate test classes per layer (ControllerTests, ServiceTests)
- Use meaningful test names: MethodName_Scenario_ExpectedOutcome
- Keep tests isolated (no real external calls)

7. Output
- Provide complete test class code
- Include mock setup
- Include multiple test methods covering above scenarios

Do not include unnecessary boilerplate. Focus on clean, production-quality unit tests.