pipeline {
  agent any

  environment {
    DOTNET_CLI_HOME = "${WORKSPACE}"
  }

  tools {
    nodejs 'Node 21.7.3'  // Ce nom doit correspondre à ce que tu as configuré dans Jenkins > Global Tool Configuration
  }

  stages {
    stage('Checkout Code') {
      steps {
        git 'https://github.com/nessrinere/Doukkena.git'
      }
    }

    stage('Build .NET Backend') {
      steps {
        dir('Backend') {
          sh 'dotnet restore'
          sh 'dotnet build --configuration Release'
          sh 'dotnet test || true'
        }
      }
    }

    stage('Build Angular Frontend') {
      steps {
        dir('Frontend') {
          sh 'npm install'
          sh 'ng build --configuration production || ng build'
        }
      }
    }

    stage('Archive Build Artifacts') {
      steps {
        archiveArtifacts artifacts: 'Frontend/dist/**/*', allowEmptyArchive: true
        archiveArtifacts artifacts: 'Backend/bin/**/*', allowEmptyArchive: true
      }
    }
  }

  post {
    success {
      echo '✅ Build succeeded'
    }
    failure {
      echo '❌ Build failed'
    }
  }
}
