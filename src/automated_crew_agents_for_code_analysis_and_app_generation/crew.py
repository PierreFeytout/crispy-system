from crewai import Agent, Crew, Process, Task
from crewai.project import CrewBase, agent, crew, task
from crewai_tools import DirectoryReadTool
from crewai_tools import GithubSearchTool
from crewai_tools import FileReadTool

@CrewBase
class AutomatedCrewAgentsForCodeAnalysisAndAppGenerationCrew():
    """AutomatedCrewAgentsForCodeAnalysisAndAppGeneration crew"""

    @agent
    def code_analyzer(self) -> Agent:
        return Agent(
            config=self.agents_config['code_analyzer'],
            tools=[DirectoryReadTool(), GithubSearchTool(), FileReadTool()],
        )

    @agent
    def business_mockup_extractor(self) -> Agent:
        return Agent(
            config=self.agents_config['business_mockup_extractor'],
            tools=[],
        )

    @agent
    def uiux_developer(self) -> Agent:
        return Agent(
            config=self.agents_config['uiux_developer'],
            tools=[],
        )

    @agent
    def backend_developer(self) -> Agent:
        return Agent(
            config=self.agents_config['backend_developer'],
            tools=[],
        )

    @agent
    def unit_test_generator(self) -> Agent:
        return Agent(
            config=self.agents_config['unit_test_generator'],
            tools=[],
        )


    @task
    def analyze_code_task(self) -> Task:
        return Task(
            config=self.tasks_config['analyze_code_task'],
            tools=[DirectoryReadTool(), GithubSearchTool(), FileReadTool()],
        )

    @task
    def extract_business_requirements_task(self) -> Task:
        return Task(
            config=self.tasks_config['extract_business_requirements_task'],
            tools=[],
        )

    @task
    def generate_ui_components_task(self) -> Task:
        return Task(
            config=self.tasks_config['generate_ui_components_task'],
            tools=[],
        )

    @task
    def generate_backend_services_task(self) -> Task:
        return Task(
            config=self.tasks_config['generate_backend_services_task'],
            tools=[],
        )

    @task
    def generate_unit_tests_task(self) -> Task:
        return Task(
            config=self.tasks_config['generate_unit_tests_task'],
            tools=[],
        )


    @crew
    def crew(self) -> Crew:
        """Creates the AutomatedCrewAgentsForCodeAnalysisAndAppGeneration crew"""
        return Crew(
            agents=self.agents, # Automatically created by the @agent decorator
            tasks=self.tasks, # Automatically created by the @task decorator
            process=Process.sequential,
            verbose=True,
        )
