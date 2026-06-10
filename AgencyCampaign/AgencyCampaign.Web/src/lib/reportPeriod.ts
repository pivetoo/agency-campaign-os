// Os endpoints de relatorio comparam o fim do periodo de forma exclusiva (changedAt < to).
// A data "Até" (YYYY-MM-DD) precisa virar o inicio do dia SEGUINTE em UTC para que o
// ultimo dia selecionado entre no resultado; mandar meia-noite do proprio dia exclui ele.
export function periodStartIso(date: string): string {
  return new Date(date).toISOString()
}

export function periodEndExclusiveIso(date: string): string {
  const end = new Date(date)
  end.setUTCDate(end.getUTCDate() + 1)
  return end.toISOString()
}
