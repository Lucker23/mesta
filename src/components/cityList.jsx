import City from "./city/city.jsx"
import {cities} from "./cz-cities.js"

const CityList = () => {
return (
  <div>
    <table style={{border:'2px solid navy'}}>
      <caption style={{border:'2px solid navy'}}>Superhustá Tabulka Měst</caption>
      <thead>
        <tr>
          <th style={{border:'2px solid navy'}}>Mesto</th>
          <th style={{border:'2px solid navy'}}>Počet Obytel</th>
          <th style={{border:'2px solid navy'}}>Rozloha v Km2</th>
          <th style={{border:'2px solid navy'}}>Okres</th>
          <th style={{border:'2px solid navy'}}>Obrázek</th>
        </tr>
      </thead>
      <tbody>
      {cities.map((city, i) => <City city={city} key={i}/>)}
      </tbody>
    </table>
   
  </div>
)


}

;



export default CityList;