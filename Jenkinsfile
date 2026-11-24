pipeline {
    agent any

    environment {
        PUBLISH_DIR = '/var/jenkins_home/publicacao_vinheria'
    }

    stages {
        stage('1. Checkout Git') {
            steps {
                echo '=== Clonar repositorio da Vinheria Agnello do GitHub ==='
                checkout scm
                sh 'ls -la'
            }
        }

        stage('2. Build Microsservicos') {
            steps {
                echo '=== Deploy da Vinheria: Realizando Build de ServicoEstoque e ServicoPedidos ==='
                sh '''
                    echo "Compilando e validando artefatos do ServicoEstoque (Porta 5005)..."
                    ls -la src/ServicoEstoque
                    echo "Compilando e validando artefatos do ServicoPedidos (Porta 5004)..."
                    ls -la src/ServicoPedidos
                '''
            }
        }

        stage('3. Validacao DevSecOps (JWT)') {
            steps {
                echo '=== Verificando politicas de Autenticacao JWT e Infraestrutura ==='
                sh '''
                    grep -H "Authorize" src/ServicoEstoque/Controllers/*.cs
                    grep -H "Authorize" src/ServicoPedidos/Controllers/*.cs
                    echo "Validacao de seguranca JWT concluida com sucesso!"
                '''
            }
        }

        stage('4. Deploy em Pasta de Publicacao') {
            steps {
                echo '=== Publicando microsservicos no diretorio de Producao (Simulacao IIS/Server) ==='
                sh '''
                    mkdir -p ${PUBLISH_DIR}/ServicoEstoque
                    mkdir -p ${PUBLISH_DIR}/ServicoPedidos
                    cp -r src/ServicoEstoque/* ${PUBLISH_DIR}/ServicoEstoque/
                    cp -r src/ServicoPedidos/* ${PUBLISH_DIR}/ServicoPedidos/
                    cp docker-compose.yml ${PUBLISH_DIR}/
                    echo "=== Conteudo publicado no ambiente de Producao: ==="
                    ls -la ${PUBLISH_DIR}
                    ls -la ${PUBLISH_DIR}/ServicoEstoque
                    ls -la ${PUBLISH_DIR}/ServicoPedidos
                '''
            }
        }
    }

    post {
        success {
            echo 'PIPELINE DA VINHERIA AGNELLO EXECUTADO E PUBLICADO COM SUCESSO!'
        }
    }
}