$f = "C:\Abc\ATMegaPestaV1\ATMegaPestaV1\Views\TesteFuncionalidadesView.xaml"
$c = [System.IO.File]::ReadAllText($f, [System.Text.Encoding]::UTF8)

$dash   = [string][char]0x2014
$down   = [string][char]0x2193
$square = ([string][char]0x25A0) + " "

$c = $c.Replace("Ã¢â‚¬â€"", $dash)
$c = $c.Replace("Ã¢â€ â€œ", $down)
$c = $c.Replace("Ã¢â€`"Â ", $square)
$c = $c.Replace("sequÃƒÂªncia",      "sequência")
$c = $c.Replace("sequÃªncia",        "sequência")
$c = $c.Replace("RecuperaÃƒÂ§ÃƒÂ£o","Recuperação")
$c = $c.Replace("RecuperaÃ§Ã£o",    "Recuperação")
$c = $c.Replace("VerificaÃƒÂ§ÃƒÂ£o","Verificação")
$c = $c.Replace("VerificaÃ§Ã£o",    "Verificação")
$c = $c.Replace("LÃƒÂ³gica",        "Lógica")
$c = $c.Replace("LÃ³gica",          "Lógica")
$c = $c.Replace("Ãšltimo",          "Último")
$c = $c.Replace("ContinuaÃƒÂ§ÃƒÂ£o","Continuação")
$c = $c.Replace("ContinuaÃ§Ã£o",   "Continuação")

[System.IO.File]::WriteAllText($f, $c, (New-Object System.Text.UTF8Encoding $false))
Write-Host "Done"
