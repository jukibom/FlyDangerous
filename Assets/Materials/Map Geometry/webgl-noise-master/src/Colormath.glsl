float4 mixcol (float4 col1, float4 col2, float fac)
{
   fac = 0.5 + 0.5 * cos((1-fac) * 3.14159);
   return col1*fac + col2*(1.0-fac);
   // return 1.0;

}