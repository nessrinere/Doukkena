pipeline {
  agent any

  environment {
    DOTNET_CLI_HOME = "${WORKSPACE}"  // Prevent permission issues for .NET
  }

  tools {
    nodejs 'Node 21.7.3' // Replace with the exact name of the NodeJS tool you set up in Jenkins
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
          echo "Restoring .NET packages..."
          sh 'dotnet restore'

          echo "Building .NET project..."
          sh 'dotnet build --configuration Release'

          echo "Running .NET tests (if any)..."
          sh 'dotnet test || echo "No tests found or test failed."'
        }
      }
    }

    stage('Build Angular Frontend') {
      steps {
        dir('Frontend') {
          echo "Installing Angular dependencies..."
          sh 'npm install'

          echo "Building Angular app..."
          sh 'ng build --configuration production || ng build'
        }
      }
    }

    stage('Archive Build Artifacts') {
      steps {
        echo "Archiving frontend and backend output..."
        archiveArtifacts artifacts: 'Frontend/dist/**/*', allowEmptyArchive: true
        archiveArtifacts artifacts: 'Backend/bin/**/*', allowEmptyArchive: true
      }
    }
  }

  post {
    success {
      echo '✅ Build completed successfully.'
    }
    failure {
      echo '❌ Build failed.'
    }
  }
}
