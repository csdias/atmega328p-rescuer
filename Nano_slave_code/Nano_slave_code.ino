#include <SPI.h>

volatile char comandoRecebido;
volatile bool novoDado = false;
unsigned long tempoUltimaResposta = 0;
String mensagemEnviar = "";
int index = 0;

void setup() {
  Serial.begin(9600);
  pinMode(MISO, OUTPUT);
  SPCR |= _BV(SPE); // Ativa modo escravo
  SPI.attachInterrupt();
  tempoUltimaResposta = millis();
}

ISR(SPI_STC_vect) {
  comandoRecebido = SPDR;
  novoDado = true;
}

void loop() {
  if (novoDado) {
    if (comandoRecebido == 'O') { // Recebeu pedido de "Ola"
      unsigned long agora = millis();
      long decorrido = (agora - tempoUltimaResposta) / 1000;
      mensagemEnviar = "ola master. Decorreram " + String(decorrido) + "s\n";
      tempoUltimaResposta = agora;
      index = 0;
    } 
    else if (comandoRecebido == 'X') { // Recebeu pedido de encerramento
      Serial.println("Recebido comando de saída. Respondendo: adeus chefe");
      mensagemEnviar = "adeus chefe\n";
      index = 0;
    }

    // Prepara o próximo byte para o Mestre "puxar"
    if (index < mensagemEnviar.length()) {
      SPDR = mensagemEnviar[index++];
    } else {
      SPDR = '\0';
    }
    novoDado = false;
  }
}