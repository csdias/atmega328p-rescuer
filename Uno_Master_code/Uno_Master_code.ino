#include <SPI.h>

// Pinos de Controlo do 74HC244 (via transístor NOT)
const int pinoUSBasp = 7; // HIGH ativa USBasp
const int pinoUnoSPI = 8; // HIGH ativa comunicaçao Uno-Nano
const int pinoSS     = 10; // Slave Select para o Nano

unsigned long ultimoEnvio = 0;
bool modoSPIAtivo = false;

void setup() {
  // Configuração dos pinos de controlo
  pinMode(pinoUSBasp, OUTPUT);
  pinMode(pinoUnoSPI, OUTPUT);
  pinMode(pinoSS, OUTPUT);
  
  // Estado inicial: Tudo desligado (conforme pedido)
  digitalWrite(pinoUSBasp, LOW);
  digitalWrite(pinoUnoSPI, LOW);
  digitalWrite(pinoSS, HIGH); // SS em HIGH (desativado)
  
  Serial.begin(9600);
  SPI.begin();
  SPI.setClockDivider(SPI_CLOCK_DIV16); // Frequência de 1MHz (estável)
  
  exibirMenu();
}

void loop() {
  // 1. Processamento de comandos do Monitor Serial
  if (Serial.available() > 0) {
    String comando = Serial.readString();
    comando.trim();
    comando.toUpperCase();

    // Lógica para USBasp (Pino 7)
    if (comando == "L") {
      if (modoSPIAtivo) encerrarComunicacao(); // Se o Uno estava a falar, despede-se
      
      if (digitalRead(pinoUnoSPI) == HIGH) {
        digitalWrite(pinoUnoSPI, LOW);
        Serial.println("A aguardar comutação (1s)...");
        delay(1000);
      }
      digitalWrite(pinoUSBasp, HIGH);
      Serial.println(">>> USBASP ATIVADO");
    } 
    else if (comando == "D") {
      digitalWrite(pinoUSBasp, LOW);
      Serial.println(">>> USBASP DESATIVADO");
    }

    // Lógica para SPI Uno (Pino 8)
    else if (comando == "LU") {
      if (digitalRead(pinoUSBasp) == HIGH) {
        digitalWrite(pinoUSBasp, LOW);
        Serial.println("A aguardar comutação (1s)...");
        delay(1000);
      }
      digitalWrite(pinoUnoSPI, HIGH);
      modoSPIAtivo = true;
      ultimoEnvio = millis();
      Serial.println(">>> SPI UNO ATIVADO (Mestre)");
    } 
    else if (comando == "DU") {
      if (modoSPIAtivo) {
        encerrarComunicacao();
        digitalWrite(pinoUnoSPI, LOW);
        modoSPIAtivo = false;
        Serial.println(">>> SPI UNO DESATIVADO");
      }
    }
  }

  // 2. Comunicação Automática (5 em 5 segundos)
  if (modoSPIAtivo && (millis() - ultimoEnvio >= 5000)) {
    comunicarComNano();
    ultimoEnvio = millis();
  }
}

// Função de comunicação SPI robusta
void comunicarComNano() {
  digitalWrite(pinoSS, LOW); // Garante que o SS do Nano baixa
  delay(1); 

  SPI.transfer('O');    // Envia o comando de "Olá"
  delay(2);             // Tempo para o Nano processar a interrupção
  
  Serial.print("Nano respondeu: ");
  for(int i = 0; i < 40; i++) { // Aumentado para 40 para frases longas
    char c = SPI.transfer(0x00); // "Puxa" os caracteres do Nano
    if(c == '\n' || c == '\0') break;
    Serial.print(c);
    delay(1);
  }
  Serial.println();
  
  digitalWrite(pinoSS, HIGH); // Liberta o bus
}

// Função de despedida antes de desligar o barramento
void encerrarComunicacao() {
  Serial.println("Enviando comando de despedida...");
  digitalWrite(pinoSS, LOW);
  SPI.transfer('X'); // 'X' código para "Adeus chefe" no Nano
  delay(5);          // Tempo para o Nano carregar a string de adeus
  
  Serial.print("Mensagem final: ");
  for(int i = 0; i < 20; i++) {
    char c = SPI.transfer(0x00);
    if(c == '\n' || c == '\0') break;
    Serial.print(c);
    delay(1);
  }
  Serial.println();
  digitalWrite(pinoSS, HIGH);
  delay(10); // Estabilização
}

void exibirMenu() {
  Serial.println("\n--- GESTOR DE BARRAMENTO & SPI MASTER ---");
  Serial.println("L  / D  -> Ligar/Desligar USBasp");
  Serial.println("LU / DU -> Ligar/Desligar SPI Uno");
  Serial.println("-----------------------------------------");
}